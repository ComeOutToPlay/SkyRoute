using SkyRoute.Application.Abstractions;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Infrastructure.Data;

// Hardcoded catalogue, per challenge.md §3.1 ("hardcode at least 6 airports across at least 2
// countries") and docs/02-revision.md's exact list: 6 airports, 4 countries.
public sealed class AirportCatalog : IAirportCatalog
{
    private static readonly IReadOnlyList<Airport> Airports =
    [
        new Airport("JFK", "New York", "United States", "US"),
        new Airport("LAX", "Los Angeles", "United States", "US"),
        new Airport("ORD", "Chicago", "United States", "US"),
        new Airport("LHR", "London", "United Kingdom", "GB"),
        new Airport("CDG", "Paris", "France", "FR"),
        new Airport("FCO", "Rome", "Italy", "IT")
    ];

    public Airport? FindByCode(string code) =>
        Airports.FirstOrDefault(a => string.Equals(a.Code, code, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Airport> GetAll() => Airports;
}
