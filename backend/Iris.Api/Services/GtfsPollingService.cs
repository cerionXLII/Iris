using System.Diagnostics;
using Iris.Core.Interfaces;
using Iris.Core.Services;
using Microsoft.AspNetCore.SignalR;
using Iris.Api.Hubs;

namespace Iris.Api.Services;

public sealed class GtfsPollingService : BackgroundService
{
    private readonly IVehicleStore _store;
    private readonly IGtfsRealtimeParser _parser;
    private readonly IHubContext<VehicleHub> _hub;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GtfsPollingService> _logger;
    private readonly string _gtfsApiKey;

    private static readonly TimeSpan VehiclePositionInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan TripUpdateInterval = TimeSpan.FromSeconds(15);

    private const string VehiclePositionsUrl =
        "https://opendata.samtrafiken.se/gtfs-rt/sweden/VehiclePositions.pb?key={0}";
    private const string TripUpdatesUrl =
        "https://opendata.samtrafiken.se/gtfs-rt/sweden/TripUpdates.pb?key={0}";

    public GtfsPollingService(
        IVehicleStore store,
        IGtfsRealtimeParser parser,
        IHubContext<VehicleHub> hub,
        IHttpClientFactory httpClientFactory,
        ILogger<GtfsPollingService> logger,
        IConfiguration configuration)
    {
        _store = store;
        _parser = parser;
        _hub = hub;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _gtfsApiKey = configuration["GTFS_SWEDEN_3_REALTIME_API_KEY"]
            ?? throw new InvalidOperationException("GTFS_SWEDEN_3_REALTIME_API_KEY is not set in appsettings.Local.json.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GTFS polling service started.");

        var tripUpdateTimer = Stopwatch.StartNew();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollVehiclePositionsAsync(stoppingToken);

                if (tripUpdateTimer.Elapsed >= TripUpdateInterval)
                {
                    await PollTripUpdatesAsync(stoppingToken);
                    tripUpdateTimer.Restart();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Error during GTFS polling cycle, will retry.");
            }

            await Task.Delay(VehiclePositionInterval, stoppingToken);
        }
    }

    private async Task PollVehiclePositionsAsync(CancellationToken ct)
    {
        var url = string.Format(VehiclePositionsUrl, _gtfsApiKey);
        var data = await FetchBytesAsync(url, ct);
        if (data is null) return;

        var positions = _parser.ParseVehiclePositions(data);
        _store.UpdatePositions(positions);

        var snapshots = _store.GetAll();
        await _hub.Clients.All.SendAsync("VehicleUpdate", snapshots, ct);

        _logger.LogDebug("Pushed {Count} vehicle positions.", snapshots.Count);
    }

    private async Task PollTripUpdatesAsync(CancellationToken ct)
    {
        var url = string.Format(TripUpdatesUrl, _gtfsApiKey);
        var data = await FetchBytesAsync(url, ct);
        if (data is null) return;

        var delays = _parser.ParseTripDelays(data);
        _store.UpdateTripDelays(delays);

        _logger.LogDebug("Updated {Count} trip delays.", delays.Count);
    }

    private async Task<byte[]?> FetchBytesAsync(string url, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("gtfs");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-protobuf"));
            using var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for {Url} — status: {Status}", SanitizeUrl(url), ex.StatusCode);
            return null;
        }
    }

    // Remove API key from URLs before logging
    private static string SanitizeUrl(string url)
    {
        var idx = url.IndexOf("key=", StringComparison.Ordinal);
        return idx >= 0 ? url[..idx] + "key=***" : url;
    }
}
