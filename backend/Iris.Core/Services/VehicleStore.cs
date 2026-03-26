using System.Collections.Concurrent;
using Iris.Core.Interfaces;
using Iris.Core.Models;

namespace Iris.Core.Services;

public sealed class VehicleStore : IVehicleStore
{
    private readonly ConcurrentDictionary<string, VehicleSnapshot> _vehicles = new();
    private readonly ConcurrentDictionary<string, int> _tripDelays = new();

    public int Count => _vehicles.Count;

    public IReadOnlyCollection<VehicleSnapshot> GetAll() =>
        _vehicles.Values.ToList();

    public void UpdatePositions(IEnumerable<VehiclePosition> positions)
    {
        foreach (var pos in positions)
        {
            var delay = pos.TripId != null && _tripDelays.TryGetValue(pos.TripId, out var d) ? d : (int?)null;

            _vehicles[pos.VehicleId] = new VehicleSnapshot(
                VehicleId: pos.VehicleId,
                Latitude: pos.Latitude,
                Longitude: pos.Longitude,
                Bearing: pos.Bearing,
                Speed: pos.Speed,
                VehicleType: pos.VehicleType,
                RouteShortName: pos.RouteId,
                Headsign: null,
                AgencyName: null,
                DelaySeconds: delay,
                Timestamp: pos.Timestamp
            );
        }

        // Remove vehicles not seen in last 60 seconds
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-60);
        foreach (var key in _vehicles.Keys.ToList())
        {
            if (_vehicles.TryGetValue(key, out var v) && v.Timestamp < cutoff)
                _vehicles.TryRemove(key, out _);
        }
    }

    public void UpdateTripDelays(IEnumerable<(string TripId, int DelaySeconds)> delays)
    {
        foreach (var (tripId, delay) in delays)
            _tripDelays[tripId] = delay;
    }
}
