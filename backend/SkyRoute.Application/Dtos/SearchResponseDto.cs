namespace SkyRoute.Application.Dtos;

// isInternational is present here, at the response root (computed once per search from
// FlightSearchCriteria), and deliberately absent from FlightOfferDto — confirmed
// architectural decision, see docs/03-execution-plan.md Phase 3 and FlightOffer.cs.
public sealed record SearchResponseDto(
    Guid SearchId,
    int PassengerCount,
    string Currency,
    bool IsInternational,
    IReadOnlyList<FlightOfferDto> Flights);
