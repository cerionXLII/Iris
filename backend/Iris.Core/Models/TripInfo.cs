namespace Iris.Core.Models;

public sealed record TripInfo(
    string TripId,
    string? RouteShortName,
    string? RouteLongName,
    string? Headsign,
    string? AgencyName,
    int? DelaySeconds
);
