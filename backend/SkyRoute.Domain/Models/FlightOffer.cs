using SkyRoute.Domain.Enums;

namespace SkyRoute.Domain.Models;

// A provider-generated flight offer. Deliberately does NOT expose IsInternational: that is a
// criteria-level fact (see FlightSearchCriteria), not a per-offer fact, and every offer within
// a given search shares the same origin/destination, so computing it here would be redundant.
public sealed class FlightOffer
{
    public required string Provider { get; init; }
    public required string FlightNumber { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }

    // Local to the departure/arrival airport, unspecified DateTimeKind, no UTC offset —
    // avoids the display bug of a flight appearing to depart "a day early" in the browser's
    // local timezone.
    public required DateTime DepartureTime { get; init; }
    public required DateTime ArrivalTime { get; init; }

    public required int DurationMinutes { get; init; }
    public required CabinClass CabinClass { get; init; }
    public required decimal PricePerPassenger { get; init; }
}
