using SkyRoute.Application.Abstractions;
using SkyRoute.Application.Dtos;
using SkyRoute.Application.Exceptions;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Enums;
using SkyRoute.Domain.Models;
using SkyRoute.Domain.Rules;

namespace SkyRoute.Application.Services;

public sealed class BookingService(ISearchOfferCache searchOfferCache, IBookingStore bookingStore)
{
    private const string Currency = "USD";
    private const string ReferencePrefix = "SR-";
    private const string ReferenceAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public Task<BookingResponseDto> BookAsync(BookingRequestDto request)
    {
        var cachedSearch = searchOfferCache.Get(request.SearchId)
            ?? throw new OfferExpiredException(request.SearchId);

        var selectedOffer = cachedSearch.Offers
            .FirstOrDefault(offer => string.Equals(offer.Id, request.FlightId, StringComparison.Ordinal));
        if (selectedOffer is null)
        {
            throw new FlightNotFoundException(request.FlightId);
        }

        if (request.Passengers.Count != cachedSearch.Criteria.PassengerCount)
        {
            throw new ValidationException(
                nameof(request.Passengers),
                "Passenger count does not match the selected search.");
        }

        var validationErrors = ValidatePassengerDocuments(request.Passengers, cachedSearch.Criteria.IsInternational);
        if (validationErrors.Count > 0)
        {
            throw new ValidationException("One or more validation errors occurred.", validationErrors);
        }

        var totalPrice = selectedOffer.PricePerPassenger * cachedSearch.Criteria.PassengerCount;
        var passengers = request.Passengers
            .Select(passenger => new Passenger
            {
                FullName = passenger.FullName,
                Email = passenger.Email,
                DocumentNumber = passenger.DocumentNumber
            })
            .ToList();

        var booking = CreateAndSaveBooking(selectedOffer, passengers, totalPrice);

        return Task.FromResult(new BookingResponseDto(
            BookingReference: booking.Reference,
            Status: booking.Status,
            FlightSummary: MapToFlightSummaryDto(booking.FlightOffer, cachedSearch.Criteria.PassengerCount),
            PricePerPassenger: booking.FlightOffer.PricePerPassenger,
            PassengerCount: cachedSearch.Criteria.PassengerCount,
            TotalPrice: booking.TotalPrice,
            Currency: booking.Currency));
    }

    private Booking CreateAndSaveBooking(
        FlightOffer selectedOffer,
        IReadOnlyList<Passenger> passengers,
        decimal totalPrice)
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var reference = GenerateReference();
            if (bookingStore.FindByReference(reference) is not null)
            {
                continue;
            }

            var booking = new Booking
            {
                Reference = reference,
                FlightOffer = CloneOffer(selectedOffer),
                Passengers = passengers,
                TotalPrice = totalPrice,
                Currency = Currency,
                Status = BookingStatus.Confirmed,
                CreatedAtUtc = DateTime.UtcNow
            };

            try
            {
                bookingStore.Save(booking);
                return booking;
            }
            catch (InvalidOperationException)
            {
                // Rare concurrent collision on the generated reference; retry.
            }
        }

        throw new InvalidOperationException("Could not generate a unique booking reference.");
    }

    private static Dictionary<string, string[]> ValidatePassengerDocuments(
        IReadOnlyList<PassengerDto> passengers,
        bool isInternational)
    {
        var errors = new Dictionary<string, string[]>();

        for (var index = 0; index < passengers.Count; index++)
        {
            var passenger = passengers[index];
            if (DocumentValidator.IsValid(passenger.DocumentNumber, isInternational))
            {
                continue;
            }

            errors[$"passengers[{index}].documentNumber"] =
            [
                isInternational
                    ? "Document number must be a valid passport format."
                    : "Document number must be a valid national ID format."
            ];
        }

        return errors;
    }

    private static FlightOffer CloneOffer(FlightOffer offer) => new()
    {
        Provider = offer.Provider,
        FlightNumber = offer.FlightNumber,
        Origin = offer.Origin,
        Destination = offer.Destination,
        DepartureTime = offer.DepartureTime,
        ArrivalTime = offer.ArrivalTime,
        DurationMinutes = offer.DurationMinutes,
        CabinClass = offer.CabinClass,
        PricePerPassenger = offer.PricePerPassenger
    };

    private static FlightOfferDto MapToFlightSummaryDto(FlightOffer offer, int passengerCount) => new(
        Id: offer.Id,
        Provider: offer.Provider,
        FlightNumber: offer.FlightNumber,
        Origin: offer.Origin,
        Destination: offer.Destination,
        DepartureTime: offer.DepartureTime,
        ArrivalTime: offer.ArrivalTime,
        DurationMinutes: offer.DurationMinutes,
        CabinClass: offer.CabinClass,
        PricePerPassenger: offer.PricePerPassenger,
        TotalPrice: offer.PricePerPassenger * passengerCount);

    private static string GenerateReference()
    {
        Span<char> value = stackalloc char[ReferencePrefix.Length + 6];
        ReferencePrefix.AsSpan().CopyTo(value);
        for (var index = ReferencePrefix.Length; index < value.Length; index++)
        {
            value[index] = ReferenceAlphabet[Random.Shared.Next(ReferenceAlphabet.Length)];
        }

        return value.ToString();
    }
}
