namespace Iris.Core.Models;

public sealed record VehiclePosition(
    string VehicleId,
    double Latitude,
    double Longitude,
    float? Bearing,
    float? Speed,
    string? RouteId,
    string? TripId,
    string? Label,
    VehicleType VehicleType,
    DateTimeOffset Timestamp
);
