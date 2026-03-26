using Google.Protobuf;
using Iris.Core.Interfaces;
using Iris.Core.Models;

namespace Iris.Core.Services;

public sealed class GtfsRealtimeParser : IGtfsRealtimeParser
{
    private static readonly Dictionary<int, VehicleType> RouteTypeMap = new()
    {
        { 0, VehicleType.Tram },
        { 1, VehicleType.Metro },
        { 2, VehicleType.Train },
        { 3, VehicleType.Bus },
        { 4, VehicleType.Ferry },
        { 5, VehicleType.CableCar },
        { 6, VehicleType.Gondola },
        { 7, VehicleType.Funicular },
        { 100, VehicleType.Train },
        { 101, VehicleType.Train },
        { 102, VehicleType.Train },
        { 105, VehicleType.Train },
        { 106, VehicleType.Train },
        { 109, VehicleType.Train },
        { 200, VehicleType.Bus },
        { 700, VehicleType.Bus },
        { 900, VehicleType.Tram },
        { 1000, VehicleType.Ferry },
        { 1300, VehicleType.Ferry },
        { 1500, VehicleType.Ferry },
    };

    public IReadOnlyList<VehiclePosition> ParseVehiclePositions(byte[] data)
    {
        var feed = TransitRealtime.FeedMessage.Parser.ParseFrom(data);
        var results = new List<VehiclePosition>(feed.Entity.Count);

        foreach (var entity in feed.Entity)
        {
            // Message fields: null check. Scalar fields: HasXxx.
            if (entity.IsDeleted || entity.Vehicle is not { } vp)
                continue;

            if (vp.Position is not { } pos)
                continue;

            var vehicleId = vp.Vehicle is { } vd && vd.HasId
                ? vd.Id
                : entity.HasId ? entity.Id : null;

            if (string.IsNullOrWhiteSpace(vehicleId))
                continue;

            var label = vp.Vehicle is { } vd2 && vd2.HasLabel ? vd2.Label : null;
            var routeId = vp.Trip is { } trip && trip.HasRouteId ? trip.RouteId : null;
            var tripId = vp.Trip is { } trip2 && trip2.HasTripId ? trip2.TripId : null;

            var timestamp = vp.HasTimestamp
                ? DateTimeOffset.FromUnixTimeSeconds((long)vp.Timestamp)
                : DateTimeOffset.UtcNow;

            results.Add(new VehiclePosition(
                VehicleId: vehicleId,
                Latitude: pos.Latitude,
                Longitude: pos.Longitude,
                Bearing: pos.HasBearing ? pos.Bearing : null,
                Speed: pos.HasSpeed ? pos.Speed : null,
                RouteId: routeId,
                TripId: tripId,
                Label: label,
                VehicleType: VehicleType.Bus, // enriched later from static data
                Timestamp: timestamp
            ));
        }

        return results;
    }

    public IReadOnlyList<(string TripId, int DelaySeconds)> ParseTripDelays(byte[] data)
    {
        var feed = TransitRealtime.FeedMessage.Parser.ParseFrom(data);
        var results = new List<(string, int)>(feed.Entity.Count);

        foreach (var entity in feed.Entity)
        {
            if (entity.IsDeleted || entity.TripUpdate is not { } tu)
                continue;

            if (tu.Trip is not { } trip || !trip.HasTripId)
                continue;

            if (tu.HasDelay)
            {
                results.Add((trip.TripId, (int)tu.Delay));
                continue;
            }

            var nextStop = tu.StopTimeUpdate
                .FirstOrDefault(s => s.Departure is { } d && d.HasDelay);

            if (nextStop?.Departure is { } dep)
                results.Add((trip.TripId, dep.Delay));
        }

        return results;
    }

    public static VehicleType RouteTypeToVehicleType(int routeType) =>
        RouteTypeMap.TryGetValue(routeType, out var vt) ? vt : VehicleType.Bus;
}
