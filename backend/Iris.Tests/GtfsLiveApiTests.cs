using System.Text.Json;
using FluentAssertions;
using Google.Protobuf;
using TransitRealtime;

namespace Iris.Tests;

/// <summary>
/// Live integration tests that hit the real Trafiklab API.
/// Run with: dotnet test --filter Category=Live
/// Requires GTFS_SWEDEN_3_REALTIME_API_KEY to be subscribed on Trafiklab.
/// </summary>
[Trait("Category", "Live")]
public class GtfsLiveApiTests
{
    private const string VehiclePositionsUrl =
        "https://opendata.samtrafiken.se/gtfs-rt/sweden/VehiclePositions.pb?key={0}";
    private const string TripUpdatesUrl =
        "https://opendata.samtrafiken.se/gtfs-rt/sweden/TripUpdates.pb?key={0}";

    private static readonly string ApiKey = LoadApiKey();

    private static string LoadApiKey()
    {
        // Read from environment variable first, then fall back to appsettings.Local.json
        var fromEnv = Environment.GetEnvironmentVariable("GTFS_SWEDEN_3_REALTIME_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv;

        var localJson = Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json");
        if (!File.Exists(localJson)) return "";

        var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(localJson));
        return doc.RootElement.TryGetProperty("GTFS_SWEDEN_3_REALTIME_API_KEY", out var v)
            ? v.GetString() ?? ""
            : "";
    }

    private static HttpClient BuildClient() =>
        new(new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        })
        {
            Timeout = TimeSpan.FromSeconds(15),
            DefaultRequestHeaders = { { "User-Agent", "Iris-Transport-Map/1.0" } }
        };

    [Fact]
    public void ApiKey_Is_Configured()
    {
        ApiKey.Should().NotBeNullOrWhiteSpace(
            "GTFS_SWEDEN_3_REALTIME_API_KEY must be set in appsettings.Local.json");
    }

    [Fact]
    public async Task VehiclePositions_Returns_200_And_Valid_Protobuf()
    {
        ApiKey.Should().NotBeNullOrWhiteSpace("API key must be configured");

        using var client = BuildClient();
        var url = string.Format(VehiclePositionsUrl, ApiKey);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-protobuf"));

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            $"Trafiklab returned {(int)response.StatusCode}. " +
            "If 403: the API key is not subscribed to 'GTFS Sweden 3 Realtime' on the Trafiklab portal.");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty("response body should contain protobuf data");

        var feed = FeedMessage.Parser.ParseFrom(bytes);
        feed.Should().NotBeNull();
        feed.Entity.Should().NotBeEmpty("expected at least one vehicle in the feed");
    }

    [Fact]
    public async Task TripUpdates_Returns_200_And_Valid_Protobuf()
    {
        ApiKey.Should().NotBeNullOrWhiteSpace("API key must be configured");

        using var client = BuildClient();
        var url = string.Format(TripUpdatesUrl, ApiKey);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-protobuf"));

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            $"Trafiklab returned {(int)response.StatusCode}. " +
            "If 403: the API key is not subscribed to 'GTFS Sweden 3 Realtime' on the Trafiklab portal.");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();

        var feed = FeedMessage.Parser.ParseFrom(bytes);
        feed.Should().NotBeNull();
    }

    [Fact]
    public async Task VehiclePositions_Feed_Contains_Swedish_Coordinates()
    {
        ApiKey.Should().NotBeNullOrWhiteSpace("API key must be configured");

        using var client = BuildClient();
        var url = string.Format(VehiclePositionsUrl, ApiKey);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/x-protobuf"));

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var feed = FeedMessage.Parser.ParseFrom(bytes);

        var positions = feed.Entity
            .Where(e => e.Vehicle?.Position != null)
            .Select(e => e.Vehicle!.Position!)
            .ToList();

        positions.Should().NotBeEmpty("feed should contain vehicles with GPS positions");

        // Sweden bounding box: lat 55–70, lon 10–25
        var inSweden = positions.Where(p =>
            p.Latitude is >= 55f and <= 70f &&
            p.Longitude is >= 10f and <= 25f).ToList();

        inSweden.Should().NotBeEmpty(
            $"expected vehicles within Sweden bounds, got {positions.Count} positions total");
    }
}
