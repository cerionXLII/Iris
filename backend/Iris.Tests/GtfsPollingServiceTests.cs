using System.Net;
using FluentAssertions;
using Google.Protobuf;
using TransitRealtime;
using CoreVehiclePosition = Iris.Core.Models.VehiclePosition;
using Iris.Api.Hubs;
using Iris.Api.Services;
using Iris.Core.Interfaces;
using Iris.Core.Models;
using Iris.Core.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Iris.Tests;

public class GtfsPollingServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IHubContext<VehicleHub> BuildHubMock()
    {
        var clientProxyMock = new Mock<IClientProxy>();
        clientProxyMock
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);

        var hubMock = new Mock<IHubContext<VehicleHub>>();
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);
        return hubMock.Object;
    }

    private static IConfiguration BuildConfig(string gtfsKey = "test-gtfs-key") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GTFS_SWEDEN_3_REALTIME_API_KEY"] = gtfsKey,
            })
            .Build();

    private static (GtfsPollingService service, Mock<HttpMessageHandler> handler) BuildService(
        HttpResponseMessage httpResponse,
        IVehicleStore? store = null,
        IGtfsRealtimeParser? parser = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("gtfs")).Returns(httpClient);

        var hubMock = BuildHubMock();

        var service = new GtfsPollingService(
            store ?? new VehicleStore(),
            parser ?? new GtfsRealtimeParser(),
            hubMock,
            factoryMock.Object,
            NullLogger<GtfsPollingService>.Instance,
            BuildConfig());

        return (service, handlerMock);
    }

    private static HttpResponseMessage OkProtobuf(byte[]? body = null) =>
        new(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(body ?? [])
        };

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_Throws_When_ApiKey_Missing()
    {
        var config = new ConfigurationBuilder().Build(); // empty
        var act = () => new GtfsPollingService(
            new VehicleStore(),
            new GtfsRealtimeParser(),
            new Mock<IHubContext<VehicleHub>>().Object,
            new Mock<IHttpClientFactory>().Object,
            NullLogger<GtfsPollingService>.Instance,
            config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*GTFS_SWEDEN_3_REALTIME_API_KEY*");
    }

    // ── Request headers ───────────────────────────────────────────────────────

    [Fact]
    public async Task FetchBytes_Sends_Accept_Protobuf_Header()
    {
        HttpRequestMessage? captured = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(OkProtobuf());

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("gtfs")).Returns(httpClient);

        var service = new GtfsPollingService(
            new VehicleStore(),
            new GtfsRealtimeParser(),
            BuildHubMock(),
            factoryMock.Object,
            NullLogger<GtfsPollingService>.Instance,
            BuildConfig());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        try { await service.StartAsync(cts.Token); await Task.Delay(100, cts.Token); } catch { }

        captured.Should().NotBeNull();
        captured!.Headers.Accept.Should().Contain(h => h.MediaType == "application/x-protobuf");
    }

    [Fact]
    public async Task FetchBytes_Includes_ApiKey_In_Url()
    {
        HttpRequestMessage? captured = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(OkProtobuf());

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("gtfs")).Returns(httpClient);

        var service = new GtfsPollingService(
            new VehicleStore(),
            new GtfsRealtimeParser(),
            BuildHubMock(),
            factoryMock.Object,
            NullLogger<GtfsPollingService>.Instance,
            BuildConfig("my-test-key"));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        try { await service.StartAsync(cts.Token); await Task.Delay(100, cts.Token); } catch { }

        captured!.RequestUri!.Query.Should().Contain("key=my-test-key");
    }

    // ── HTTP error handling ───────────────────────────────────────────────────

    [Fact]
    public async Task Http_406_Does_Not_Crash_Service()
    {
        var (service, _) = BuildService(new HttpResponseMessage(HttpStatusCode.NotAcceptable));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var act = async () =>
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(250, cts.Token);
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Http_500_Does_Not_Crash_Service()
    {
        var (service, _) = BuildService(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var act = async () =>
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(250, cts.Token);
        };

        await act.Should().NotThrowAsync();
    }

    // ── Data flow ─────────────────────────────────────────────────────────────

    private static byte[] BuildMinimalFeed()
    {
        var feed = new TransitRealtime.FeedMessage
        {
            Header = new TransitRealtime.FeedHeader { GtfsRealtimeVersion = "2.0" }
        };
        return feed.ToByteArray();
    }

    [Fact]
    public async Task Successful_Poll_Pushes_Vehicles_To_Store()
    {
        var feed = BuildMinimalFeed();

        var storeMock = new Mock<IVehicleStore>();
        storeMock.Setup(s => s.GetAll()).Returns([]);

        var parserMock = new Mock<IGtfsRealtimeParser>();
        parserMock
            .Setup(p => p.ParseVehiclePositions(It.IsAny<byte[]>()))
            .Returns([
                new CoreVehiclePosition("v1", 59.3, 18.06, null, null, null, null, null, VehicleType.Bus, DateTimeOffset.UtcNow),
                new CoreVehiclePosition("v2", 57.7, 11.97, null, null, null, null, null, VehicleType.Bus, DateTimeOffset.UtcNow),
            ]);

        var (service, _) = BuildService(OkProtobuf(feed), storeMock.Object, parserMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        try { await service.StartAsync(cts.Token); await Task.Delay(250, cts.Token); } catch { }

        storeMock.Verify(s => s.UpdatePositions(It.Is<IEnumerable<CoreVehiclePosition>>(l => l.Count() == 2)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Empty_Response_Body_Does_Not_Push_To_Store()
    {
        var storeMock = new Mock<IVehicleStore>();
        var parserMock = new Mock<IGtfsRealtimeParser>();
        parserMock.Setup(p => p.ParseVehiclePositions(It.IsAny<byte[]>())).Returns([]);

        var (service, _) = BuildService(OkProtobuf([]), storeMock.Object, parserMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        try { await service.StartAsync(cts.Token); await Task.Delay(250, cts.Token); } catch { }

        // Parser called but store receives empty list — no positions pushed
        storeMock.Verify(s => s.UpdatePositions(It.Is<IEnumerable<CoreVehiclePosition>>(l => l.Count() == 0)), Times.AtLeastOnce);
    }
}
