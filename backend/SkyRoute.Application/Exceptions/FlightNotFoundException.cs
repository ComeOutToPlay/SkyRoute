namespace SkyRoute.Application.Exceptions;

// Thrown by BookingService when a FlightId is not found within an otherwise valid, cached
// search. Mapped to HTTP 404 by the WebApi exception middleware (Phase 5).
public sealed class FlightNotFoundException(string flightId)
    : Exception($"Flight '{flightId}' was not found in the requested search.")
{
    public string FlightId { get; } = flightId;
}
