using FluentAssertions;
using Iris.Core.Models;
using Iris.Core.Services;

namespace Iris.Tests;

public class VehicleStoreTests
{
    private readonly VehicleStore _store = new();

    private static VehiclePosition MakePosition(string id, double lat = 59.0, double lon = 18.0) =>
        new(id, lat, lon, null, null, null, null, null, VehicleType.Bus, DateTimeOffset.UtcNow);

    [Fact]
    public void UpdatePositions_Stores_New_Vehicles()
    {
        _store.UpdatePositions([MakePosition("v1"), MakePosition("v2")]);

        _store.Count.Should().Be(2);
        _store.GetAll().Should().Contain(v => v.VehicleId == "v1");
        _store.GetAll().Should().Contain(v => v.VehicleId == "v2");
    }

    [Fact]
    public void UpdatePositions_Overwrites_Existing_Vehicle()
    {
        _store.UpdatePositions([MakePosition("v1", 59.0, 18.0)]);
        _store.UpdatePositions([MakePosition("v1", 60.0, 19.0)]);

        _store.Count.Should().Be(1);
        var v = _store.GetAll().Single(x => x.VehicleId == "v1");
        v.Latitude.Should().BeApproximately(60.0, 0.001);
        v.Longitude.Should().BeApproximately(19.0, 0.001);
    }

    [Fact]
    public void UpdateTripDelays_Applied_To_Subsequent_Position_Update()
    {
        _store.UpdateTripDelays([("trip-1", 90)]);

        var pos = new VehiclePosition("v1", 59.0, 18.0, null, null, null, "trip-1", null, VehicleType.Bus, DateTimeOffset.UtcNow);
        _store.UpdatePositions([pos]);

        var snapshot = _store.GetAll().Single();
        snapshot.DelaySeconds.Should().Be(90);
    }

    [Fact]
    public void GetAll_Returns_ReadOnly_Collection()
    {
        _store.UpdatePositions([MakePosition("v1")]);

        var result = _store.GetAll();
        result.Should().BeAssignableTo<IReadOnlyCollection<VehicleSnapshot>>();
    }

    [Fact]
    public void UpdatePositions_Removes_Stale_Vehicles()
    {
        // Create a position that's well past the 60-second cutoff
        var stalePos = new VehiclePosition(
            "stale", 59.0, 18.0, null, null, null, null, null,
            VehicleType.Bus, DateTimeOffset.UtcNow.AddSeconds(-120)
        );
        var freshPos = MakePosition("fresh");

        // First add stale, then trigger cleanup via a fresh update
        _store.UpdatePositions([stalePos]);
        _store.UpdatePositions([freshPos]);

        _store.GetAll().Should().NotContain(v => v.VehicleId == "stale");
        _store.GetAll().Should().Contain(v => v.VehicleId == "fresh");
    }

    [Fact]
    public void Count_Reflects_Current_Vehicle_Count()
    {
        _store.Count.Should().Be(0);
        _store.UpdatePositions([MakePosition("v1"), MakePosition("v2"), MakePosition("v3")]);
        _store.Count.Should().Be(3);
    }
}
