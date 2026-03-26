using Iris.Api.Hubs;
using Iris.Api.Services;
using Iris.Core.Interfaces;
using Iris.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// ── Services ────────────────────────────────────────────────────────────────

builder.Services.AddSingleton<IVehicleStore, VehicleStore>();
builder.Services.AddSingleton<IGtfsRealtimeParser, GtfsRealtimeParser>();
builder.Services.AddHttpClient("gtfs", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "Iris-Transport-Map/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

builder.Services.AddSignalR(options =>
{
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
});

builder.Services.AddHostedService<GtfsPollingService>();
builder.Services.AddHostedService<SpaProcessManager>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:4173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // required for SignalR
    });
});

// ── App ──────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseCors("frontend");
app.UseHttpsRedirection();

// Health + stats endpoint
app.MapGet("/health", (IVehicleStore store) => Results.Ok(new
{
    status = "ok",
    vehicleCount = store.Count,
    timestamp = DateTimeOffset.UtcNow
}));

// Full snapshot REST endpoint (for initial load if needed)
app.MapGet("/api/vehicles", (IVehicleStore store) => Results.Ok(store.GetAll()));

// SignalR hub
app.MapHub<VehicleHub>("/hubs/vehicles");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
