using SkyRoute.Application.Abstractions;
using SkyRoute.Application.Dtos;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Services;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Enums;
using SkyRoute.Domain.Models;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Tests.Application;

public class BookingServiceTests
{
    private sealed class FakeSearchOfferCache : ISearchOfferCache
    {
        private readonly Dictionary<Guid, CachedSearch> _cache = new();

        public void Store(Guid searchId, FlightSearchCriteria criteria, IReadOnlyList<FlightOffer> offers) =>
            _cache[searchId] = new CachedSearch(criteria, offers);

        public CachedSearch? Get(Guid searchId) => _cache.GetValueOrDefault(searchId);
    }

    private sealed class FakeBookingStore : IBookingStore
    {
        private readonly Dictionary<string, Booking> _bookings = new(StringComparer.OrdinalIgnoreCase);

        public void Save(Booking booking)
        {
            if (_bookings.ContainsKey(booking.Reference))
            {
                throw new InvalidOperationException("Duplicate booking reference.");
            }

            _bookings[booking.Reference] = booking;
        }

        public Booking? FindByReference(string reference) => _bookings.GetValueOrDefault(reference);
    }

    private static readonly Airport Jfk = new("JFK", "New York", "United States", "US");
    private static readonly Airport Lax = new("LAX", "Los Angeles", "United States", "US");
    private static readonly Airport Lhr = new("LHR", "London", "United Kingdom", "GB");

    [Fact]
    public async Task BookAsync_SucceedsForInternationalRoute_WithValidPassport()
    {
        var searchId = Guid.NewGuid();
        var (cache, _) = BuildCache(
            searchId,
            BuildCriteria(Jfk, Lhr, passengerCount: 1),
            [BuildOffer("GlobalAir", "GA100", Jfk.Code, Lhr.Code, CabinClass.Economy, 320.50m)]);

        var service = new BookingService(cache, new FakeBookingStore());

        var response = await service.BookAsync(new BookingRequestDto(
            searchId,
            "globalair-GA100-20251201-Economy",
            [new PassengerDto("Jane Doe", "jane@example.com", "X1234567")]));

        Assert.StartsWith("SR-", response.BookingReference);
        Assert.Equal(BookingStatus.Confirmed, response.Status);
        Assert.Equal(320.50m, response.PricePerPassenger);
        Assert.Equal(320.50m, response.TotalPrice);
    }

    [Fact]
    public async Task BookAsync_SucceedsForDomesticRoute_WithValidNationalId()
    {
        var searchId = Guid.NewGuid();
        var (cache, _) = BuildCache(
            searchId,
            BuildCriteria(Jfk, Lax, passengerCount: 1),
            [BuildOffer("BudgetWings", "BW200", Jfk.Code, Lax.Code, CabinClass.Economy, 162.00m)]);

        var service = new BookingService(cache, new FakeBookingStore());

        var response = await service.BookAsync(new BookingRequestDto(
            searchId,
            "budgetwings-BW200-20251201-Economy",
            [new PassengerDto("John Roe", "john@example.com", "123456789")]));

        Assert.Equal(BookingStatus.Confirmed, response.Status);
        Assert.Equal(162.00m, response.TotalPrice);
    }

    [Fact]
    public async Task BookAsync_ThrowsValidationException_ForNationalIdOnInternationalRoute()
    {
        var searchId = Guid.NewGuid();
        var (cache, _) = BuildCache(
            searchId,
            BuildCriteria(Jfk, Lhr, passengerCount: 1),
            [BuildOffer("GlobalAir", "GA100", Jfk.Code, Lhr.Code, CabinClass.Economy, 320.50m)]);

        var service = new BookingService(cache, new FakeBookingStore());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.BookAsync(new BookingRequestDto(
                searchId,
                "globalair-GA100-20251201-Economy",
                [new PassengerDto("Jane Doe", "jane@example.com", "123456789")])));

        Assert.Contains("passengers[0].documentNumber", exception.Errors.Keys);
    }

    [Fact]
    public async Task BookAsync_ThrowsValidationException_ForPassportOnDomesticRoute()
    {
        var searchId = Guid.NewGuid();
        var (cache, _) = BuildCache(
            searchId,
            BuildCriteria(Jfk, Lax, passengerCount: 1),
            [BuildOffer("GlobalAir", "GA210", Jfk.Code, Lax.Code, CabinClass.Economy, 207.00m)]);

        var service = new BookingService(cache, new FakeBookingStore());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.BookAsync(new BookingRequestDto(
                searchId,
                "globalair-GA210-20251201-Economy",
                [new PassengerDto("John Roe", "john@example.com", "X1234567")])));

        Assert.Contains("passengers[0].documentNumber", exception.Errors.Keys);
    }

    [Fact]
    public async Task BookAsync_ThrowsOfferExpiredException_ForUnknownSearchId()
    {
        var service = new BookingService(new FakeSearchOfferCache(), new FakeBookingStore());

        await Assert.ThrowsAsync<OfferExpiredException>(() =>
            service.BookAsync(new BookingRequestDto(
                Guid.NewGuid(),
                "globalair-GA100-20251201-Economy",
                [new PassengerDto("Jane Doe", "jane@example.com", "X1234567")])));
    }

    [Fact]
    public async Task BookAsync_ThrowsFlightNotFoundException_WhenFlightIdIsMissingInCachedSearch()
    {
        var searchId = Guid.NewGuid();
        var (cache, _) = BuildCache(
            searchId,
            BuildCriteria(Jfk, Lhr, passengerCount: 1),
            [BuildOffer("GlobalAir", "GA100", Jfk.Code, Lhr.Code, CabinClass.Economy, 320.50m)]);

        var service = new BookingService(cache, new FakeBookingStore());

        await Assert.ThrowsAsync<FlightNotFoundException>(() =>
            service.BookAsync(new BookingRequestDto(
                searchId,
                "globalair-GA999-20251201-Economy",
                [new PassengerDto("Jane Doe", "jane@example.com", "X1234567")])));
    }

    [Fact]
    public async Task BookAsync_ThrowsValidationException_WhenPassengerCountMismatchesCachedSearch()
    {
        var searchId = Guid.NewGuid();
        var (cache, _) = BuildCache(
            searchId,
            BuildCriteria(Jfk, Lhr, passengerCount: 2),
            [BuildOffer("GlobalAir", "GA100", Jfk.Code, Lhr.Code, CabinClass.Economy, 320.50m)]);

        var service = new BookingService(cache, new FakeBookingStore());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.BookAsync(new BookingRequestDto(
                searchId,
                "globalair-GA100-20251201-Economy",
                [new PassengerDto("Jane Doe", "jane@example.com", "X1234567")])));

        Assert.Contains(nameof(BookingRequestDto.Passengers), exception.Errors.Keys);
    }

    [Fact]
    public async Task BookAsync_ComputesTotalPrice_FromCachedOfferAndPassengerCount()
    {
        var searchId = Guid.NewGuid();
        var (cache, _) = BuildCache(
            searchId,
            BuildCriteria(Jfk, Lhr, passengerCount: 3),
            [BuildOffer("GlobalAir", "GA100", Jfk.Code, Lhr.Code, CabinClass.Business, 100.00m)]);

        var service = new BookingService(cache, new FakeBookingStore());

        var response = await service.BookAsync(new BookingRequestDto(
            searchId,
            "globalair-GA100-20251201-Business",
            [
                new PassengerDto("A", "a@example.com", "X1234567"),
                new PassengerDto("B", "b@example.com", "Y1234567"),
                new PassengerDto("C", "c@example.com", "Z1234567")
            ]));

        Assert.Equal(100.00m, response.PricePerPassenger);
        Assert.Equal(3, response.PassengerCount);
        Assert.Equal(300.00m, response.TotalPrice);
    }

    private static (FakeSearchOfferCache cache, CachedSearch cachedSearch) BuildCache(
        Guid searchId,
        FlightSearchCriteria criteria,
        IReadOnlyList<FlightOffer> offers)
    {
        var cache = new FakeSearchOfferCache();
        cache.Store(searchId, criteria, offers);
        return (cache, new CachedSearch(criteria, offers));
    }

    private static FlightSearchCriteria BuildCriteria(Airport origin, Airport destination, int passengerCount) => new()
    {
        Origin = origin,
        Destination = destination,
        DepartureDate = new DateOnly(2025, 12, 1),
        PassengerCount = passengerCount,
        CabinClass = CabinClass.Economy
    };

    private static FlightOffer BuildOffer(
        string provider,
        string flightNumber,
        string origin,
        string destination,
        CabinClass cabinClass,
        decimal pricePerPassenger) => new()
    {
        Provider = provider,
        FlightNumber = flightNumber,
        Origin = origin,
        Destination = destination,
        DepartureTime = new DateOnly(2025, 12, 1).ToDateTime(new TimeOnly(10, 0)),
        ArrivalTime = new DateOnly(2025, 12, 1).ToDateTime(new TimeOnly(14, 0)),
        DurationMinutes = 240,
        CabinClass = cabinClass,
        PricePerPassenger = pricePerPassenger
    };
}
