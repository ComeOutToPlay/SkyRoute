using SkyRoute.Domain.Enums;

namespace SkyRoute.Application.Dtos;

// FlightSummary reuses FlightOfferDto: the execution plan does not define a separate
// "flight summary" shape, and FlightOfferDto already carries exactly what challenge.md §3.3
// asks the booking screen to summarize (route, provider, times, cabin class) plus pricing.
// Introducing a second, near-identical DTO here would be an unapproved addition.
public sealed record BookingResponseDto(
    string BookingReference,
    BookingStatus Status,
    FlightOfferDto FlightSummary,
    decimal PricePerPassenger,
    int PassengerCount,
    decimal TotalPrice,
    string Currency);
