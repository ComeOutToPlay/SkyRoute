using SkyRoute.Application.Dtos;

namespace SkyRoute.Application.Services;

// Orchestration only. Full implementation (resolve airports via IAirportCatalog, fan out to
// IFlightProvider instances, cache the result via ISearchOfferCache, map to SearchResponseDto)
// is completed in Phase 5, once providers, the catalogue, and the cache exist (Phase 4).
public sealed class FlightSearchService
{
    public Task<SearchResponseDto> SearchAsync(FlightSearchRequestDto request, CancellationToken ct)
    {
        throw new NotImplementedException("Implemented in Phase 5 — Search API.");
    }
}
