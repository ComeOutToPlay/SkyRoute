namespace SkyRoute.Application.Dtos;

// No price, no route data, and no isInternational flag are sent by the client — the server
// derives all of that from the FlightSearchCriteria cached under SearchId. This is the
// approved booking price-integrity design (docs/02-revision.md §1 / §"POST /api/bookings").
public sealed record BookingRequestDto(
    Guid SearchId,
    string FlightId,
    IReadOnlyList<PassengerDto> Passengers);
