using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Application.Abstractions;

// Not a new architectural layer: FlightSearchService needs to resolve airport codes, and
// BookingService needs the same catalogue via the cached criteria. The concrete
// implementation (hardcoded catalogue) lives in Infrastructure (Phase 4); without this seam,
// Application would either depend on Infrastructure directly (wrong dependency direction)
// or duplicate the airport list.
public interface IAirportCatalog
{
    Airport? FindByCode(string code);

    IReadOnlyList<Airport> GetAll();
}
