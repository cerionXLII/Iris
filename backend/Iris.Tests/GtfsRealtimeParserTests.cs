using FluentAssertions;
using Google.Protobuf;
using Iris.Core.Models;
using Iris.Core.Services;
using GtfsVehiclePosition = TransitRealtime.VehiclePosition;
using TransitRealtime;

namespace Iris.Tests;

public class GtfsRealtimeParserTests
{
    private readonly GtfsRealtimeParser _parser = new();

    private static byte[] BuildVehiclePositionFeed(params (string id, float lat, float lon, float? bearing, float? speed, string? routeId, string? tripId)[] vehicles)
    {
        var feed = new FeedMessage
        {
            Header = new FeedHeader
            {
                GtfsRealtimeVersion = "2.0",
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };

        foreach (var (id, lat, lon, bearing, speed, routeId, tripId) in vehicles)
        {
            var vp = new GtfsVehiclePosition
            {
                Vehicle = new VehicleDescriptor { Id = id },
                Position = new Position { Latitude = lat, Longitude = lon }
            };

            if (bearing.HasValue) vp.Position.Bearing = bearing.Value;
            if (speed.HasValue) vp.Position.Speed = speed.Value;

            if (routeId != null || tripId != null)
            {
                vp.Trip = new TripDescriptor();
                if (routeId != null) vp.Trip.RouteId = routeId;
                if (tripId != null) vp.Trip.TripId = tripId;
            }

            feed.Entity.Add(new FeedEntity { Id = id, Vehicle = vp });
        }

        return feed.ToByteArray();
    }

    private static byte[] BuildTripUpdateFeed(params (string tripId, int delay)[] trips)
    {
        var feed = new FeedMessage
        {
            Header = new FeedHeader { GtfsRealtimeVersion = "2.0" }
        };

        foreach (var (tripId, delay) in trips)
        {
            var tu = new TripUpdate
            {
                Trip = new TripDescriptor { TripId = tripId },
                Delay = delay
            };

            feed.Entity.Add(new FeedEntity { Id = tripId, TripUpdate = tu });
        }

        return feed.ToByteArray();
    }

    [Fact]
    public void ParseVehiclePositions_Returns_All_Valid_Vehicles()
    {
        var data = BuildVehiclePositionFeed(
            ("bus-1", 59.3f, 18.06f, 45f, 12.5f, "route-A", "trip-1"),
            ("bus-2", 57.7f, 11.97f, null, null, "route-B", null)
        );

        var result = _parser.ParseVehiclePositions(data);

        result.Should().HaveCount(2);
        result[0].VehicleId.Should().Be("bus-1");
        result[0].Latitude.Should().BeApproximately(59.3, 0.001);
        result[0].Longitude.Should().BeApproximately(18.06, 0.001);
        result[0].Bearing.Should().BeApproximately(45f, 0.001f);
        result[0].Speed.Should().BeApproximately(12.5f, 0.001f);
        result[0].RouteId.Should().Be("route-A");
        result[0].TripId.Should().Be("trip-1");
        result[1].VehicleId.Should().Be("bus-2");
        result[1].Bearing.Should().BeNull();
        result[1].Speed.Should().BeNull();
    }

    [Fact]
    public void ParseVehiclePositions_Skips_Deleted_Entities()
    {
        var feed = new FeedMessage
        {
            Header = new FeedHeader { GtfsRealtimeVersion = "2.0" }
        };
        feed.Entity.Add(new FeedEntity
        {
            Id = "del-1",
            IsDeleted = true,
            Vehicle = new GtfsVehiclePosition
            {
                Vehicle = new VehicleDescriptor { Id = "del-1" },
                Position = new Position { Latitude = 59f, Longitude = 18f }
            }
        });

        var result = _parser.ParseVehiclePositions(feed.ToByteArray());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseVehiclePositions_Skips_Entities_Without_Position()
    {
        var feed = new FeedMessage
        {
            Header = new FeedHeader { GtfsRealtimeVersion = "2.0" }
        };
        feed.Entity.Add(new FeedEntity
        {
            Id = "no-pos",
            Vehicle = new GtfsVehiclePosition
            {
                Vehicle = new VehicleDescriptor { Id = "no-pos" }
                // No Position set
            }
        });

        var result = _parser.ParseVehiclePositions(feed.ToByteArray());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseVehiclePositions_EmptyFeed_Returns_Empty()
    {
        var feed = new FeedMessage
        {
            Header = new FeedHeader { GtfsRealtimeVersion = "2.0" }
        };

        var result = _parser.ParseVehiclePositions(feed.ToByteArray());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseTripDelays_Returns_Delays_For_All_Trips()
    {
        var data = BuildTripUpdateFeed(
            ("trip-1", 120),
            ("trip-2", -30),
            ("trip-3", 0)
        );

        var result = _parser.ParseTripDelays(data);

        result.Should().HaveCount(3);
        result.Should().Contain(("trip-1", 120));
        result.Should().Contain(("trip-2", -30));
        result.Should().Contain(("trip-3", 0));
    }

    [Fact]
    public void ParseTripDelays_Skips_Entries_Without_TripId()
    {
        var feed = new FeedMessage
        {
            Header = new FeedHeader { GtfsRealtimeVersion = "2.0" }
        };
        feed.Entity.Add(new FeedEntity
        {
            Id = "e1",
            TripUpdate = new TripUpdate
            {
                Trip = new TripDescriptor() // No TripId
            }
        });

        var result = _parser.ParseTripDelays(feed.ToByteArray());

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, VehicleType.Tram)]
    [InlineData(1, VehicleType.Metro)]
    [InlineData(2, VehicleType.Train)]
    [InlineData(3, VehicleType.Bus)]
    [InlineData(4, VehicleType.Ferry)]
    [InlineData(109, VehicleType.Train)]
    [InlineData(999, VehicleType.Bus)] // unknown → defaults to bus
    public void RouteTypeToVehicleType_Maps_Correctly(int routeType, VehicleType expected)
    {
        GtfsRealtimeParser.RouteTypeToVehicleType(routeType).Should().Be(expected);
    }
}
