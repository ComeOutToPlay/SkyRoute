namespace SkyRoute.Application.Exceptions;

// Thrown by BookingService when a SearchId is not found in ISearchOfferCache (cache miss or
// expired 10-minute TTL). Mapped to HTTP 409 by the WebApi exception middleware (Phase 5).
public sealed class OfferExpiredException(Guid searchId)
    : Exception($"Search '{searchId}' has expired or does not exist. Please search again.")
{
    public Guid SearchId { get; } = searchId;
}
