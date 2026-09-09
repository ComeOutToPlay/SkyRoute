using SkyRoute.Domain.Enums;
using SkyRoute.Domain.Models;
using SkyRoute.Domain.ValueObjects;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Tests.Infrastructure;

public class GlobalAirProviderTests
{
    private static readonly Airport Jfk = new("JFK", "New York", "United States", "US");
    private static readonly Airport Lax = new("LAX", "Los Angeles", "United States", "US");
    private static readonly Airport OrdAirport = new("ORD", "Chicago", "United States", "US");
    private static readonly Airport Fco = new("FCO", "Rome", "Italy", "IT");

    private static FlightSearchCriteria Criteria(Airport origin, Airport destination, CabinClass cabinClass) => new()
    {
        Origin = origin,
        Destination = destination,
        DepartureDate = new DateOnly(2025, 12, 1),
        PassengerCount = 1,
        CabinClass = cabinClass
    };

    [Fact]
    public async Task SearchAsync_AppliesFifteenPercentSurcharge_RoundedToTwoDecimals()
    {
        var provider = new GlobalAirProvider();
        // JFK<->LAX base fare is 180.00 (RouteFareTable); Economy multiplier is 1.0.
        // 180.00 * 1.15 = 207.00 exactly, no midpoint rounding ambiguity in this case.
        var offers = await provider.SearchAsync(Criteria(Jfk, Lax, CabinClass.Economy), CancellationToken.None);

        Assert.NotEmpty(offers);
        Assert.All(offers, o => Assert.Equal(207.00m, o.PricePerPassenger));
    }

    [Fact]
    public async Task SearchAsync_RoundsToTwoDecimals_ForTheApprovedRouteFareTable()
    {
        var provider = new GlobalAirProvider();
        // JFK<->ORD base fare is 30.00; Economy multiplier 1.0.
        // 30.00 * 1.15 = 34.50 exactly -- none of the approved RouteFareTable values combined
        // with the three cabin multipliers produce a true midpoint case (see
        // RouteFareTableRoundingTests for the synthetic away-from-zero midpoint proof).
        var offers = await provider.SearchAsync(Criteria(Jfk, OrdAirport, CabinClass.Economy), CancellationToken.None);

        Assert.All(offers, o => Assert.Equal(34.50m, o.PricePerPassenger));
    }

    [Fact]
    public async Task SearchAsync_AppliesCabinMultiplier_BeforeProviderRule()
    {
        var provider = new GlobalAirProvider();
        // JFK<->LAX base fare 180.00; Business multiplier 2.5 -> 450.00; +15% -> 517.50.
        var economyOffers = await provider.SearchAsync(Criteria(Jfk, Lax, CabinClass.Economy), CancellationToken.None);
        var businessOffers = await provider.SearchAsync(Criteria(Jfk, Lax, CabinClass.Business), CancellationToken.None);

        Assert.All(economyOffers, o => Assert.Equal(207.00m, o.PricePerPassenger));
        Assert.All(businessOffers, o => Assert.Equal(517.50m, o.PricePerPassenger));
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForUncoveredRoute()
    {
        var provider = new GlobalAirProvider();
        // ORD<->FCO is the deliberately uncovered pair (no entry in RouteFareTable).
        var offers = await provider.SearchAsync(Criteria(OrdAirport, Fco, CabinClass.Economy), CancellationToken.None);

        Assert.Empty(offers);
    }
}
