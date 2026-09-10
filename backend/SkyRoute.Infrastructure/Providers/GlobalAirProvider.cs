using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.Models;

namespace SkyRoute.Infrastructure.Providers;

// Pricing rule lives inside the provider that owns it, per challenge.md §2 ("each provider
// applies its own rules") — no separate IPricingStrategy indirection (docs/02-revision.md).
public sealed class GlobalAirProvider : IFlightProvider
{
    public string ProviderName => "GlobalAir";

    public async Task<IReadOnlyList<FlightOffer>> SearchAsync(FlightSearchCriteria criteria, CancellationToken ct)
    {
        await Task.Delay(400, ct); // simulated network latency — makes the loading indicator observable

        var baseFare = RouteFareTable.GetBaseFare(criteria.Origin.Code, criteria.Destination.Code);
        if (baseFare is null)
        {
            return []; // GlobalAir has no coverage for this route at all (e.g. ORD<->FCO)
        }

        var fareWithCabin = baseFare.Value * RouteFareTable.CabinMultiplier(criteria.CabinClass);
        var pricePerPassenger = RouteFareTable.RoundAwayFromZero(fareWithCabin * 1.15m);

        var durationMinutes = RouteFareTable.GetTypicalDurationMinutes(criteria.Origin.Code, criteria.Destination.Code);

        // Deterministic: same criteria (route + date + cabin) always yields the same seed, so
        // repeated searches within a session return the same flight numbers/times.
        var seed = HashCode.Combine(criteria.Origin.Code, criteria.Destination.Code, criteria.DepartureDate, criteria.CabinClass, ProviderName);
        var rng = new Random(seed);

        return Enumerable.Range(0, 2)
            .Select(index =>
            {
                var departureHour = rng.Next(6, 21); // 06:00-20:59 departure window
                var departureTime = criteria.DepartureDate.ToDateTime(new TimeOnly(departureHour, 0));
                var offerDurationMinutes = durationMinutes + (index * 10);

                return new FlightOffer
                {
                    Provider = ProviderName,
                    FlightNumber = $"GA{100 + Math.Abs(seed % 800) + index}",
                    Origin = criteria.Origin.Code,
                    Destination = criteria.Destination.Code,
                    DepartureTime = departureTime,
                    ArrivalTime = departureTime.AddMinutes(offerDurationMinutes),
                    DurationMinutes = offerDurationMinutes,
                    CabinClass = criteria.CabinClass,
                    PricePerPassenger = pricePerPassenger
                };
            })
            .ToList();
    }
}
