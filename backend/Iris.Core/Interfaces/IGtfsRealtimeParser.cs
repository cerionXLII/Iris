using Iris.Core.Models;

namespace Iris.Core.Interfaces;

public interface IGtfsRealtimeParser
{
    IReadOnlyList<VehiclePosition> ParseVehiclePositions(byte[] data);
    IReadOnlyList<(string TripId, int DelaySeconds)> ParseTripDelays(byte[] data);
}
