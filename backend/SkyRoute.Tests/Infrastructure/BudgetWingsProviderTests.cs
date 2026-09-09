using SkyRoute.Domain.Enums;
using SkyRoute.Domain.Models;
using SkyRoute.Domain.ValueObjects;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Tests.Infrastructure;

public class BudgetWingsProviderTests
{
    private static readonly Airport Jfk = new("JFK", "New York", "United States", "US");
    private static readonly Airport Lax = new("LAX", "Los Angeles", "United States", "US");
    private static readonly Airport OrdAirport = new("ORD", "Chicago", "United States", "US");
    private static readonly Airport Lhr = new("LHR", "London", "United Kingdom", "GB");
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
    public async Task SearchAsync_AppliesTenPercentDiscount_ToBaseFareOnly()
    {
        var provider = new BudgetWingsProvider();
        // JFK<->LAX base fare is 180.00 (RouteFareTable); Economy multiplier 1.0.
        // 180.00 * 0.90 = 162.00, well above the 29.99 floor.
        var offers = await provider.SearchAsync(Criteria(Jfk, Lax, CabinClass.Economy), CancellationToken.None);

        Assert.NotEmpty(offers);
        Assert.All(offers, o => Assert.Equal(162.00m, o.PricePerPassenger));
    }

    [Fact]
    public async Task SearchAsync_EnforcesTheDollarFloor_OnTheCheapRoute()
    {
        var provider = new BudgetWingsProvider();
        // JFK<->ORD base fare is 30.00 (deliberately low, per RouteFareTable's own remarks).
        // 30.00 * 0.90 = 27.00, which is below 29.99 -> floor must apply.
        var offers = await provider.SearchAsync(Criteria(Jfk, OrdAirport, CabinClass.Economy), CancellationToken.None);

        Assert.NotEmpty(offers);
        Assert.All(offers, o => Assert.Equal(29.99m, o.PricePerPassenger));
    }

    [Fact]
    public async Task SearchAsync_DiscountIsComputedFromBaseFare_NotFromAnAlreadyDiscountedValue()
    {
        var provider = new BudgetWingsProvider();
        // If the discount were (incorrectly) applied twice, JFK<->LAX Economy would be
        // 180.00 * 0.90 * 0.90 = 145.80 instead of the correct 162.00.
        var offers = await provider.SearchAsync(Criteria(Jfk, Lax, CabinClass.Economy), CancellationToken.None);

        Assert.All(offers, o => Assert.NotEqual(145.80m, o.PricePerPassenger));
        Assert.All(offers, o => Assert.Equal(162.00m, o.PricePerPassenger));
    }

    [Fact]
    public async Task SearchAsync_AppliesCabinMultiplier_BeforeProviderRule()
    {
        var provider = new BudgetWingsProvider();
        // JFK<->LAX base fare 180.00; Business multiplier 2.5 -> 450.00; -10% -> 405.00.
        var economyOffers = await provider.SearchAsync(Criteria(Jfk, Lax, CabinClass.Economy), CancellationToken.None);
        var businessOffers = await provider.SearchAsync(Criteria(Jfk, Lax, CabinClass.Business), CancellationToken.None);

        Assert.All(economyOffers, o => Assert.Equal(162.00m, o.PricePerPassenger));
        Assert.All(businessOffers, o => Assert.Equal(405.00m, o.PricePerPassenger));
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForFirstClass()
    {
        var provider = new BudgetWingsProvider();
        var offers = await provider.SearchAsync(Criteria(Jfk, Lax, CabinClass.First), CancellationToken.None);

        Assert.Empty(offers);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForLongHaulRoutes()
    {
        var provider = new BudgetWingsProvider();
        // JFK<->LHR is a long-haul route per RouteFareTable.IsLongHaul.
        var offers = await provider.SearchAsync(Criteria(Jfk, Lhr, CabinClass.Economy), CancellationToken.None);

        Assert.Empty(offers);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForUncoveredRoute()
    {
        var provider = new BudgetWingsProvider();
        // ORD<->FCO is the deliberately uncovered pair (no entry in RouteFareTable).
        var offers = await provider.SearchAsync(Criteria(OrdAirport, Fco, CabinClass.Economy), CancellationToken.None);

        Assert.Empty(offers);
    }
}
