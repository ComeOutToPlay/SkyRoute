using SkyRoute.Domain.Enums;
using SkyRoute.Domain.Models;

namespace SkyRoute.Domain.Entities;

public sealed class Booking
{
    public required string Reference { get; init; }

    // Snapshot of the offer at booking time, not a live reference into the search cache —
    // the cached search may expire (10-minute TTL) long before the booking record is read.
    public required FlightOffer FlightOffer { get; init; }

    public required IReadOnlyList<Passenger> Passengers { get; init; }
    public required decimal TotalPrice { get; init; }
    public required string Currency { get; init; }
    public required BookingStatus Status { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}
