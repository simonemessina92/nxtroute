using Microsoft.Extensions.Hosting.WindowsServices;
using Nxtroute;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory });
builder.WebHost.UseUrls("http://127.0.0.1:3000");
builder.Services.AddWindowsService(options => options.ServiceName = "NXTROUTE");
builder.Services.AddSingleton<Gateway>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<Gateway>());
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(25));
var app = builder.Build();
app.Use(async (context, next) =>
{
    if (context.Request.Host.Host != "127.0.0.1" && context.Request.Host.Host != "localhost")
    {
        context.Response.StatusCode = 403;
        return;
    }
    if (context.Request.Method != "GET" &&
        (context.Request.Headers.Origin != "http://127.0.0.1:3000" && context.Request.Headers.Origin != "http://localhost:3000"))
    {
        context.Response.StatusCode = 403;
        return;
    }
    context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    using var page = typeof(Gateway).Assembly.GetManifestResourceStream("dashboard.html")!;
    await page.CopyToAsync(context.Response.Body);
});
app.MapGet("/api/channels", async (Gateway gateway) => Results.Json(await gateway.StatusAsync()));
app.MapGet("/api/system", () => Results.Json(new
{
    version = "0.1.0",
    mode = WindowsServiceHelpers.IsWindowsService() ? "Windows service" : "foreground",
    decklink = new { status = "previsto", filesDetected = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Blackmagic Design", "Blackmagic Desktop Video", "DeckLinkAPI64.dll")), note = "File Desktop Video rilevabili; schede e driver caricati non ancora verificati. Nessun driver viene installato automaticamente." },
    ndi = new { status = "previsto", note = "SDK standard: integrazione e condizioni di distribuzione da verificare." }
}));
app.MapPost("/api/channels", async (NewChannel input, Gateway gateway) =>
{
    if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 80) return Results.BadRequest(new { error = "Nome richiesto, massimo 80 caratteri." });
    try { return Results.Ok(await gateway.AddAsync(input.Name.Trim())); }
    catch (InvalidOperationException exception) { return Results.BadRequest(new { error = exception.Message }); }
});
app.MapPost("/api/channels/{id}/start", async (string id, Gateway gateway) => await gateway.SetEnabledAsync(id, true) ? Results.Ok() : Results.NotFound());
app.MapPost("/api/channels/{id}/stop", async (string id, Gateway gateway) => await gateway.SetEnabledAsync(id, false) ? Results.Ok() : Results.NotFound());
app.MapGet("/api/channels/{id}/logs", (string id, Gateway gateway) => gateway.LogTail(id) is { } lines ? Results.Json(lines) : Results.NotFound());
if (!WindowsServiceHelpers.IsWindowsService() && !builder.Configuration.GetValue<bool>("no-browser"))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://127.0.0.1:3000") { UseShellExecute = true }); }
        catch (System.ComponentModel.Win32Exception) { Console.WriteLine("Aprire http://127.0.0.1:3000 nel browser."); }
    });
}
await app.RunAsync();

record NewChannel(string Name);
