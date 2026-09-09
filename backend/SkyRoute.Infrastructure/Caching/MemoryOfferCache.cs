using Microsoft.Extensions.Caching.Memory;
using SkyRoute.Application.Abstractions;
using SkyRoute.Domain.Models;

namespace SkyRoute.Infrastructure.Caching;

// Backs ISearchOfferCache with IMemoryCache, 10-minute TTL, per the approved booking
// price-integrity design (docs/02-revision.md §1: searchId + IMemoryCache, no client-supplied
// price or route data trusted at booking time).
public sealed class MemoryOfferCache(IMemoryCache cache) : ISearchOfferCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public void Store(Guid searchId, FlightSearchCriteria criteria, IReadOnlyList<FlightOffer> offers)
    {
        cache.Set(CacheKey(searchId), new CachedSearch(criteria, offers), Ttl);
    }

    public CachedSearch? Get(Guid searchId) =>
        cache.TryGetValue(CacheKey(searchId), out CachedSearch? cached) ? cached : null;

    private static string CacheKey(Guid searchId) => $"search-offers:{searchId}";
}
