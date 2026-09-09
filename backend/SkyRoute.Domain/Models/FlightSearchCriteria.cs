using SkyRoute.Domain.Enums;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Domain.Models;

public sealed class FlightSearchCriteria
{
    public required Airport Origin { get; init; }
    public required Airport Destination { get; init; }
    public required DateOnly DepartureDate { get; init; }
    public required int PassengerCount { get; init; }
    public required CabinClass CabinClass { get; init; }

    // Derived, never stored: comparing the two airports' country codes is the single
    // source of truth for domestic vs international. This is intentionally NOT duplicated
    // on FlightOffer (see FlightOffer.cs) and is computed once per search, not once per offer.
    public bool IsInternational => Origin.CountryCode != Destination.CountryCode;
}
