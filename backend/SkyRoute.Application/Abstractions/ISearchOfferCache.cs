using SkyRoute.Domain.Models;

namespace SkyRoute.Application.Abstractions;

// Bundles the search criteria alongside the offers it produced, so BookingService can later
// derive IsInternational and PassengerCount from the same criteria the search used — never
// from anything the client sends. Backed by IMemoryCache in Infrastructure (Phase 4), with a
// 10-minute TTL per the approved booking price-integrity design (docs/02-revision.md §1).
public sealed record CachedSearch(FlightSearchCriteria Criteria, IReadOnlyList<FlightOffer> Offers);

public interface ISearchOfferCache
{
    void Store(Guid searchId, FlightSearchCriteria criteria, IReadOnlyList<FlightOffer> offers);

    CachedSearch? Get(Guid searchId);
}
