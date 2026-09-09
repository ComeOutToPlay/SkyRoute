using SkyRoute.Domain.Enums;

namespace SkyRoute.Application.Dtos;

public sealed record FlightSearchRequestDto(
    string Origin,
    string Destination,
    DateOnly DepartureDate,
    int Passengers,
    CabinClass CabinClass);
