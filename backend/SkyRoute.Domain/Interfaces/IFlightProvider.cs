using SkyRoute.Domain.Models;

namespace SkyRoute.Domain.Interfaces;

// The one interface in this codebase that earns its place on merit: two implementations
// today (GlobalAir, BudgetWings), a third promised by the brief (§2), consumed
// polymorphically via IEnumerable<IFlightProvider>. Onboarding provider #3 = one class +
// one DI registration line, no other code changes.
public interface IFlightProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<FlightOffer>> SearchAsync(FlightSearchCriteria criteria, CancellationToken ct);
}
