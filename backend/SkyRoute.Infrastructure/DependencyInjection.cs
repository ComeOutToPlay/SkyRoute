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
        services.AddSingleton<IBookingStore, InMemoryBookingStore>();

        // IEnumerable<IFlightProvider> — onboarding a third provider is one more registration
        // line here, no other code changes, per challenge.md §2 ("onboard additional
        // providers") and docs/03-execution-plan.md's architecture baseline.
        services.AddSingleton<IFlightProvider, GlobalAirProvider>();
        services.AddSingleton<IFlightProvider, BudgetWingsProvider>();

        return services;
    }
}
