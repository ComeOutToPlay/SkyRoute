using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SkyRoute.Application.Abstractions;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Infrastructure.Caching;
using SkyRoute.Infrastructure.Data;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddSingleton<IAirportCatalog, AirportCatalog>();
        services.AddSingleton<ISearchOfferCache, MemoryOfferCache>();

        // IEnumerable<IFlightProvider> — onboarding a third provider is one more registration
        // line here, no other code changes, per challenge.md §2 ("onboard additional
        // providers") and docs/03-execution-plan.md's architecture baseline.
        services.AddSingleton<IFlightProvider, GlobalAirProvider>();
        services.AddSingleton<IFlightProvider, BudgetWingsProvider>();

        // IBookingStore is intentionally NOT registered here yet: its only implementation,
        // InMemoryBookingStore, is a Phase 8 file (docs/03-execution-plan.md Phase 8, task 1)
        // and does not exist in this phase. Registering it now would require inventing a stub
        // class not listed in Phase 4's file list. Added in Phase 8 alongside the real
        // implementation.

        return services;
    }
}
