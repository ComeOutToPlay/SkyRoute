using SkyRoute.Domain.Enums;

namespace SkyRoute.Application.Dtos;

public sealed record FlightOfferDto(
    string Id,
    string Provider,
    string FlightNumber,
    string Origin,
    string Destination,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    int DurationMinutes,
    CabinClass CabinClass,
    decimal PricePerPassenger,
    decimal TotalPrice);
