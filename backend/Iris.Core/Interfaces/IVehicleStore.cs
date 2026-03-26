using Iris.Core.Models;

namespace Iris.Core.Interfaces;

public interface IVehicleStore
{
    IReadOnlyCollection<VehicleSnapshot> GetAll();
    void UpdatePositions(IEnumerable<VehiclePosition> positions);
    void UpdateTripDelays(IEnumerable<(string TripId, int DelaySeconds)> delays);
    int Count { get; }
}
