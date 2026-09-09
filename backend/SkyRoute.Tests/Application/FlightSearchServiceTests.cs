using SkyRoute.Application.Abstractions;
using SkyRoute.Application.Dtos;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Services;
using SkyRoute.Domain.Enums;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.Models;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Tests.Application;

public class FlightSearchServiceTests
{
    private static readonly Airport Jfk = new("JFK", "New York", "United States", "US");
    private static readonly Airport Lax = new("LAX", "Los Angeles", "United States", "US");
    private static readonly Airport Lhr = new("LHR", "London", "United Kingdom", "GB");

    private sealed class FakeAirportCatalog : IAirportCatalog
    {
        private readonly Dictionary<string, Airport> _airports = new(StringComparer.OrdinalIgnoreCase)
        {
            [Jfk.Code] = Jfk,
            [Lax.Code] = Lax,
            [Lhr.Code] = Lhr
        };

        public Airport? FindByCode(string code) => _airports.GetValueOrDefault(code);

        public IReadOnlyList<Airport> GetAll() => _airports.Values.ToList();
    }

    private sealed class FakeOfferCache : ISearchOfferCache
    {
        public CachedSearch? LastStored { get; private set; }

        public void Store(Guid searchId, FlightSearchCriteria criteria, IReadOnlyList<FlightOffer> offers) =>
            LastStored = new CachedSearch(criteria, offers);

        public CachedSearch? Get(Guid searchId) => LastStored;
    }

    private sealed class FakeProvider(string providerName, decimal pricePerPassenger) : IFlightProvider
    {
        public string ProviderName => providerName;

        public Task<IReadOnlyList<FlightOffer>> SearchAsync(FlightSearchCriteria criteria, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<FlightOffer>>(
            [
                new FlightOffer
                {
                    Provider = providerName,
                    FlightNumber = "XX100",
                    Origin = criteria.Origin.Code,
                    Destination = criteria.Destination.Code,
                    DepartureTime = criteria.DepartureDate.ToDateTime(new TimeOnly(10, 0)),
                    ArrivalTime = criteria.DepartureDate.ToDateTime(new TimeOnly(14, 0)),
                    DurationMinutes = 240,
                    CabinClass = criteria.CabinClass,
                    PricePerPassenger = pricePerPassenger
                }
            ]);
    }

    private sealed class ThrowingProvider : IFlightProvider
    {
        public string ProviderName => "Throwing";

        public Task<IReadOnlyList<FlightOffer>> SearchAsync(FlightSearchCriteria criteria, CancellationToken ct) =>
            throw new InvalidOperationException("Simulated provider failure.");
    }

    private static FlightSearchRequestDto Request(string origin, string destination, int passengers = 2) => new(
        origin, destination, new DateOnly(2025, 12, 1), passengers, CabinClass.Economy);

    [Fact]
    public async Task SearchAsync_ComputesTotalPrice_AsPricePerPassengerTimesPassengerCount()
    {
        var service = new FlightSearchService(
            new FakeAirportCatalog(), [new FakeProvider("GlobalAir", 200.00m)], new FakeOfferCache());

        var response = await service.SearchAsync(Request("JFK", "LAX", passengers: 3), CancellationToken.None);

        var offer = Assert.Single(response.Flights);
        Assert.Equal(200.00m, offer.PricePerPassenger);
        Assert.Equal(600.00m, offer.TotalPrice);
    }

    [Fact]
    public async Task SearchAsync_IsolatesAThrowingProvider_WithoutFailingTheWholeSearch()
    {
        var service = new FlightSearchService(
            new FakeAirportCatalog(),
            [new ThrowingProvider(), new FakeProvider("BudgetWings", 150.00m)],
            new FakeOfferCache());

        var response = await service.SearchAsync(Request("JFK", "LAX"), CancellationToken.None);

        var offer = Assert.Single(response.Flights);
        Assert.Equal("BudgetWings", offer.Provider);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyFlights_WhenNoProviderCoversTheRoute()
    {
        var service = new FlightSearchService(
            new FakeAirportCatalog(), [], new FakeOfferCache());

        var response = await service.SearchAsync(Request("JFK", "LAX"), CancellationToken.None);

        Assert.Empty(response.Flights);
    }

    [Fact]
    public async Task SearchAsync_ThrowsValidationException_ForUnknownAirportCode()
    {
        var service = new FlightSearchService(
            new FakeAirportCatalog(), [new FakeProvider("GlobalAir", 200.00m)], new FakeOfferCache());

        await Assert.ThrowsAsync<ValidationException>(
            () => service.SearchAsync(Request("JFK", "ZZZ"), CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsync_SetsIsInternational_TrueForDifferentCountries_FalseForSameCountry()
    {
        var service = new FlightSearchService(
            new FakeAirportCatalog(), [new FakeProvider("GlobalAir", 200.00m)], new FakeOfferCache());

        var international = await service.SearchAsync(Request("JFK", "LHR"), CancellationToken.None);
        var domestic = await service.SearchAsync(Request("JFK", "LAX"), CancellationToken.None);

        Assert.True(international.IsInternational);
        Assert.False(domestic.IsInternational);
    }
}
