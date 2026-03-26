namespace Iris.Core.Models;

/// <summary>
/// Combined vehicle position and trip info sent to clients.
/// </summary>
public sealed record VehicleSnapshot(
    string VehicleId,
    double Latitude,
    double Longitude,
    float? Bearing,
    float? Speed,
    VehicleType VehicleType,
    string? RouteShortName,
    string? Headsign,
    string? AgencyName,
    int? DelaySeconds,
    DateTimeOffset Timestamp
);
