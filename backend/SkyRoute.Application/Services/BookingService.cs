using SkyRoute.Application.Dtos;

namespace SkyRoute.Application.Services;

// Stub only. Full implementation (resolve SearchId via ISearchOfferCache, find FlightId,
// validate passenger count and document numbers via DocumentValidator, recompute totalPrice
// server-side, persist via IBookingStore) is completed in Phase 8, per the exact server
// sequence agreed in docs/02-revision.md §"POST /api/bookings".
public sealed class BookingService
{
    public Task<BookingResponseDto> BookAsync(BookingRequestDto request)
    {
        throw new NotImplementedException("Implemented in Phase 8 — Backend booking validation and confirmation.");
    }
}
