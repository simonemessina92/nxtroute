using System.Diagnostics;

namespace Nxtroute;

public sealed class ChildProcess : IAsyncDisposable
{
    private readonly Process process;
    private StreamWriter log;
    private readonly string logPath;
    private readonly object logGate = new();
    private readonly ProcessJob job = new();
    public int Id => process.Id;
    public bool Running => !process.HasExited;

    public ChildProcess(string binary, IEnumerable<string> arguments, string logPath)
    {
        this.logPath = logPath;
        if (File.Exists(logPath) && new FileInfo(logPath).Length > 5_000_000) File.Move(logPath, logPath + ".previous", true);
        log = new StreamWriter(new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
        var start = new ProcessStartInfo(binary) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        process = new Process { StartInfo = start };
        process.OutputDataReceived += (_, output) => Write(output.Data);
        process.ErrorDataReceived += (_, output) => Write(output.Data);
        var started = false;
        try
        {
            process.Start();
            started = true;
            job.Attach(process);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Write($"START pid={process.Id}");
        }
        catch { if (started && !process.HasExited) process.Kill(true); process.Dispose(); job.Dispose(); log.Dispose(); throw; }
    }

    private void Write(string? line)
    {
        if (line is null) return;
        lock (logGate)
        {
            if (log.BaseStream.Length > 5_000_000)
            {
                log.Dispose();
                File.Move(logPath, logPath + ".previous", true);
                log = new StreamWriter(new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
            }
            log.WriteLine($"{DateTimeOffset.Now:O} {line}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!process.HasExited)
        {
            if (Path.GetFileName(process.StartInfo.FileName).Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
            {
                try { await process.StandardInput.WriteLineAsync("q"); await process.StandardInput.FlushAsync(); }
                catch (IOException) { }
            }
            else process.CloseMainWindow();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { if (!process.HasExited) process.Kill(true); await process.WaitForExitAsync(); }
        }
        process.WaitForExit();
        Write($"EXIT code={process.ExitCode}");
        process.Dispose();
        job.Dispose();
        lock (logGate) log.Dispose();
    }
}
