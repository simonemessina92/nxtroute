using System.Diagnostics;
using System.Text.Json;

namespace Nxtroute;

public sealed record Channel(string Id, string Name, bool Enabled = true);

public sealed class Gateway : BackgroundService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(3) };
    private readonly Dictionary<string, ChildProcess> encoders = new();
    private readonly Dictionary<string, DateTime> retryAfter = new();
    private readonly string dataDir;
    private readonly string mediaBinary;
    private readonly string ffmpegBinary;
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly ILogger<Gateway> logger;
    private List<Channel> channels = [];
    private ChildProcess? router;
    private DateTime routerRetryAfter;
    private bool shuttingDown;

    public Gateway(IConfiguration configuration, ILogger<Gateway> logger)
    {
        this.logger = logger;
        var dataRoot = Microsoft.Extensions.Hosting.WindowsServices.WindowsServiceHelpers.IsWindowsService()
            ? Environment.SpecialFolder.CommonApplicationData : Environment.SpecialFolder.LocalApplicationData;
        dataDir = Path.GetFullPath(configuration["data-dir"] ?? Path.Combine(Environment.GetFolderPath(dataRoot), "NXTROUTE"));
        mediaBinary = configuration["mediamtx"] ?? Path.Combine(AppContext.BaseDirectory, "components", "mediamtx.exe");
        ffmpegBinary = configuration["ffmpeg"] ?? Path.Combine(AppContext.BaseDirectory, "components", "ffmpeg.exe");
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(Path.Combine(dataDir, "logs"));
        var file = Path.Combine(dataDir, "channels.json");
        channels = File.Exists(file) ? JsonSerializer.Deserialize<List<Channel>>(await File.ReadAllTextAsync(file, cancellationToken), json) ?? [] : [new("demo", "Test locale 720p25 + tono 1 kHz")];
        if (channels.Count > 8 || channels.Select(channel => channel.Id).Distinct().Count() != channels.Count || channels.Any(channel => !System.Text.RegularExpressions.Regex.IsMatch(channel.Id, "^(demo|test-[a-f0-9]{8})$"))) throw new InvalidDataException("Invalid channel configuration");
        await SaveAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await gate.WaitAsync(stoppingToken);
                try
                {
                    if (router?.Running != true && DateTime.UtcNow >= routerRetryAfter)
                    {
                        if (router is not null) await router.DisposeAsync();
                        routerRetryAfter = DateTime.UtcNow.AddSeconds(5);
                        router = TryLaunch("mediamtx", mediaBinary, [Path.Combine(dataDir, "mediamtx.yml")]);
                    }
                    foreach (var channel in channels.Where(channel => channel.Enabled))
                    {
                        if (encoders.TryGetValue(channel.Id, out var process) && process.Running) continue;
                        if (retryAfter.GetValueOrDefault(channel.Id) > DateTime.UtcNow) continue;
                        if (process is not null) await process.DisposeAsync();
                        encoders.Remove(channel.Id);
                        retryAfter[channel.Id] = DateTime.UtcNow.AddSeconds(5);
                        if (router?.Running == true && TryLaunch(channel.Id, ffmpegBinary, SyntheticArguments(channel.Id)) is { } encoder) encoders[channel.Id] = encoder;
                    }
                }
                finally { gate.Release(); }
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private ChildProcess? TryLaunch(string name, string binary, IEnumerable<string> arguments)
    {
        try { return new ChildProcess(binary, arguments, Path.Combine(dataDir, "logs", name + ".log")); }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        {
            logger.LogWarning("Cannot launch {Name}: {Reason}", name, exception.Message);
            return null;
        }
    }

    public async Task<Channel> AddAsync(string name)
    {
        await gate.WaitAsync();
        try
        {
            if (shuttingDown) throw new InvalidOperationException("Arresto in corso.");
            if (channels.Count >= 8) throw new InvalidOperationException("La demo supporta al massimo 8 canali sintetici.");
            var channel = new Channel("test-" + Guid.NewGuid().ToString("N")[..8], name, false);
            channels.Add(channel);
            try { await SaveAsync(); }
            catch { channels.Remove(channel); throw; }
            return channel;
        }
        finally { gate.Release(); }
    }

    public async Task<bool> SetEnabledAsync(string id, bool enabled)
    {
        await gate.WaitAsync();
        try
        {
            if (shuttingDown) return false;
            var index = channels.FindIndex(channel => channel.Id == id);
            if (index < 0) return false;
            var previous = channels[index];
            channels[index] = previous with { Enabled = enabled };
            try { await SaveAsync(); }
            catch { channels[index] = previous; throw; }
            if (!enabled && encoders.Remove(id, out var process)) await process.DisposeAsync();
            retryAfter.Remove(id);
            return true;
        }
        finally { gate.Release(); }
    }

    private async Task SaveAsync()
    {
        var file = Path.Combine(dataDir, "channels.json");
        await File.WriteAllTextAsync(file + ".tmp", JsonSerializer.Serialize(channels, json));
        File.Move(file + ".tmp", file, true);
        var configuration = """
            logLevel: info
            logDestinations: [stdout]
            api: true
            apiAddress: 127.0.0.1:9997
            rtspTransports: [tcp]
            rtspAddress: 127.0.0.1:8554
            rtmpAddress: 127.0.0.1:1935
            srtAddress: 127.0.0.1:8890
            hlsAddress: 127.0.0.1:8888
            hlsAlwaysRemux: true
            webrtcAddress: 127.0.0.1:8889
            webrtcLocalUDPAddress: 127.0.0.1:8189
            webrtcAdditionalHosts: [127.0.0.1]
            moq: false
            pathDefaults:
              overridePublisher: false
            paths:

            """;
        configuration += "  \"~^(demo|test-[a-f0-9]{8})/(webrtc|hls)$\":\n";
        var mediaFile = Path.Combine(dataDir, "mediamtx.yml");
        if (!File.Exists(mediaFile) || await File.ReadAllTextAsync(mediaFile) != configuration) await File.WriteAllTextAsync(mediaFile, configuration);
    }

    public async Task<object> StatusAsync()
    {
        List<Channel> snapshot;
        Dictionary<string, int?> pids;
        await gate.WaitAsync();
        try
        {
            snapshot = [.. channels];
            pids = encoders.ToDictionary(pair => pair.Key, pair => pair.Value.Running ? (int?)pair.Value.Id : null);
        }
        finally { gate.Release(); }
        var paths = new List<JsonElement>();
        var online = false;
        try
        {
            var pageCount = 1;
            for (var page = 0; page < pageCount; page++)
            {
                using var result = await client.GetAsync($"http://127.0.0.1:9997/v3/paths/list?page={page}&itemsPerPage=100");
                result.EnsureSuccessStatusCode();
                using var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
                paths.AddRange(body.RootElement.GetProperty("items").EnumerateArray().Select(item => item.Clone()));
                pageCount = body.RootElement.GetProperty("pageCount").GetInt32();
            }
            online = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException) { }
        object Rendition(string path)
        {
            var match = paths.Find(item => item.GetProperty("name").GetString() == path);
            return new { ready = match.ValueKind != JsonValueKind.Undefined && match.GetProperty("ready").GetBoolean(), tracks = match.ValueKind != JsonValueKind.Undefined && match.GetProperty("tracks").ValueKind == JsonValueKind.Array ? match.GetProperty("tracks").EnumerateArray().Select(track => track.GetString()).ToArray() : [] };
        }
        bool Ready(string path) => paths.Any(item => item.GetProperty("name").GetString() == path && item.GetProperty("ready").GetBoolean());
        return new
        {
            router = online ? "online" : "unreachable",
            channels = snapshot.Select(channel => new
            {
                channel.Id, channel.Name, channel.Enabled,
                adapter = "synthetic", status = "sperimentale", pid = pids.GetValueOrDefault(channel.Id),
                webrtcPath = channel.Id + "/webrtc", hlsPath = channel.Id + "/hls",
                ready = Ready(channel.Id + "/webrtc") && Ready(channel.Id + "/hls"),
                renditions = new { webrtc = Rendition(channel.Id + "/webrtc"), hls = Rendition(channel.Id + "/hls") }
            })
        };
    }

    public string[]? LogTail(string id)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9-]{1,40}$")) return null;
        var file = Path.Combine(dataDir, "logs", id + ".log");
        if (!File.Exists(file)) return [];
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(Math.Max(0, stream.Length - 32768), SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split('\n').TakeLast(60).ToArray();
    }

    private static string[] SyntheticArguments(string id)
    {
        string[] video = ["-c:v", "libx264", "-preset", "ultrafast", "-tune", "zerolatency", "-profile:v", "baseline", "-pix_fmt", "yuv420p", "-g", "25", "-bf", "0", "-b:v", "2000k"];
        return ["-hide_banner", "-loglevel", "warning", "-re", "-f", "lavfi", "-i", "testsrc2=size=1280x720:rate=25", "-re", "-f", "lavfi", "-i", "sine=frequency=1000:sample_rate=48000",
            "-map", "0:v", "-map", "1:a", .. video, "-c:a", "libopus", "-b:a", "64k", "-ac", "2", "-f", "rtsp", "-rtsp_transport", "tcp", $"rtsp://127.0.0.1:8554/{id}/webrtc",
            "-map", "0:v", "-map", "1:a", .. video, "-c:a", "aac", "-b:a", "128k", "-ac", "2", "-f", "rtsp", "-rtsp_transport", "tcp", $"rtsp://127.0.0.1:8554/{id}/hls"];
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await gate.WaitAsync(CancellationToken.None);
        try
        {
            shuttingDown = true;
            await Task.WhenAll(encoders.Values.Select(process => process.DisposeAsync().AsTask()));
            encoders.Clear();
            if (router is not null) await router.DisposeAsync();
        }
        finally { gate.Release(); }
    }
}
