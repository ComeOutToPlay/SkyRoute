using SkyRoute.Application.Abstractions;
using SkyRoute.Application.Dtos;
using SkyRoute.Application.Exceptions;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.Models;

namespace SkyRoute.Application.Services;

// Orchestration only: resolves airports, fans out to providers, caches the result, maps to
// the wire DTO. Pricing/coverage logic itself lives in each IFlightProvider (Phase 4).
public sealed class FlightSearchService(
    IAirportCatalog airportCatalog,
    IEnumerable<IFlightProvider> providers,
    ISearchOfferCache offerCache)
{
    private const string Currency = "USD";

    public async Task<SearchResponseDto> SearchAsync(FlightSearchRequestDto request, CancellationToken ct)
    {
        var origin = airportCatalog.FindByCode(request.Origin)
            ?? throw new ValidationException(nameof(request.Origin), $"Unknown airport code: '{request.Origin}'.");
        var destination = airportCatalog.FindByCode(request.Destination)
            ?? throw new ValidationException(nameof(request.Destination), $"Unknown airport code: '{request.Destination}'.");

        if (string.Equals(origin.Code, destination.Code, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(nameof(request.Destination), "Origin and destination must be different airports.");
        }

        var criteria = new FlightSearchCriteria
        {
            Origin = origin,
            Destination = destination,
            DepartureDate = request.DepartureDate,
            PassengerCount = request.Passengers,
            CabinClass = request.CabinClass
        };

        // Fan out to all registered providers in parallel; one provider throwing must not
        // fail the whole search (docs/03-execution-plan.md Phase 5, task 1).
        var providerTasks = providers.Select(provider => SearchProviderSafelyAsync(provider, criteria, ct));
        var providerResults = await Task.WhenAll(providerTasks);
        var offers = providerResults.SelectMany(offers => offers).ToList();

        var searchId = Guid.NewGuid();
        offerCache.Store(searchId, criteria, offers);

        return new SearchResponseDto(
            SearchId: searchId,
            PassengerCount: criteria.PassengerCount,
            Currency: Currency,
            IsInternational: criteria.IsInternational,
            Flights: offers.Select(offer => MapToDto(offer, criteria.PassengerCount)).ToList());
    }

    private static async Task<IReadOnlyList<FlightOffer>> SearchProviderSafelyAsync(
        IFlightProvider provider, FlightSearchCriteria criteria, CancellationToken ct)
    {
        try
        {
            return await provider.SearchAsync(criteria, ct);
        }
        catch (Exception)
        {
            // Isolate per-provider failures: a single misbehaving provider must not fail the
            // whole search (docs/02-revision.md's resilience note; docs/03-execution-plan.md
            // Phase 5, task 1). No logging infrastructure was introduced for this — the
            // exception is simply swallowed and the provider contributes zero offers, which
            // is the minimum behaviour the plan requires and does not add new dependencies.
            return [];
        }
    }

    private static FlightOfferDto MapToDto(FlightOffer offer, int passengerCount) => new(
        Id: offer.Id,
        Provider: offer.Provider,
        FlightNumber: offer.FlightNumber,
        Origin: offer.Origin,
        Destination: offer.Destination,
        DepartureTime: offer.DepartureTime,
        ArrivalTime: offer.ArrivalTime,
        DurationMinutes: offer.DurationMinutes,
        CabinClass: offer.CabinClass,
        PricePerPassenger: offer.PricePerPassenger,
        TotalPrice: offer.PricePerPassenger * passengerCount);
}
