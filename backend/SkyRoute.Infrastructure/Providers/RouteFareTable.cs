using SkyRoute.Domain.Enums;

namespace SkyRoute.Infrastructure.Providers;

// Shared by both providers so GlobalAir and BudgetWings prices differ only by their own
// pricing rule (+15% vs -10%/floor), never by inventing unrelated base fares — per
// docs/03-execution-plan.md Phase 4, task 2. Also the single place route coverage is
// encoded: a route absent from BaseFares is covered by NEITHER provider. ORD<->FCO is
// deliberately absent, making it the uncovered pair that makes the empty state reachable
// (docs/02-revision.md "Provider mocks").
internal static class RouteFareTable
{
    // Key is the pair of airport codes sorted ordinally, so lookups work in either direction.
    private static readonly Dictionary<(string, string), decimal> BaseFares = new()
    {
        // Domestic (US) pairs.
        [Key("JFK", "LAX")] = 180.00m,
        [Key("JFK", "ORD")] = 30.00m,  // short domestic hop: deliberately low so BudgetWings'
                                        // $29.99 floor actually engages (30.00 * 0.90 = 27.00).
        [Key("LAX", "ORD")] = 140.00m,

        // Long-haul international (US <-> Europe) pairs. BudgetWings never covers these.
        [Key("CDG", "JFK")] = 340.00m,
        [Key("CDG", "LAX")] = 400.00m,
        [Key("CDG", "ORD")] = 310.00m,
        [Key("FCO", "JFK")] = 360.00m,
        [Key("FCO", "LAX")] = 420.00m,
        [Key("JFK", "LHR")] = 320.00m,
        [Key("LAX", "LHR")] = 380.00m,
        [Key("LHR", "ORD")] = 300.00m,
        // FCO <-> ORD intentionally absent: the uncovered pair (see class remarks).

        // Short-haul international (intra-Europe) pairs.
        [Key("CDG", "FCO")] = 100.00m,
        [Key("CDG", "LHR")] = 90.00m,
        [Key("FCO", "LHR")] = 110.00m,
    };

    // Long-haul routes BudgetWings never covers, regardless of cabin.
    private static readonly HashSet<(string, string)> LongHaulRoutes =
    [
        Key("CDG", "JFK"), Key("CDG", "LAX"), Key("CDG", "ORD"),
        Key("FCO", "JFK"), Key("FCO", "LAX"),
        Key("JFK", "LHR"), Key("LAX", "LHR"), Key("LHR", "ORD")
    ];

    // Typical duration per route, shared so both providers report a consistent duration for
    // the same route (duration is a route fact, not a provider-specific guess).
    private static readonly Dictionary<(string, string), int> TypicalDurationMinutes = new()
    {
        [Key("JFK", "LAX")] = 330,
        [Key("JFK", "ORD")] = 150,
        [Key("LAX", "ORD")] = 240,
        [Key("CDG", "JFK")] = 480,
        [Key("CDG", "LAX")] = 645,
        [Key("CDG", "ORD")] = 495,
        [Key("FCO", "JFK")] = 525,
        [Key("FCO", "LAX")] = 690,
        [Key("JFK", "LHR")] = 435,
        [Key("LAX", "LHR")] = 600,
        [Key("LHR", "ORD")] = 480,
        [Key("CDG", "FCO")] = 120,
        [Key("CDG", "LHR")] = 85,
        [Key("FCO", "LHR")] = 150,
    };

    public static decimal? GetBaseFare(string origin, string destination) =>
        BaseFares.TryGetValue(Key(origin, destination), out var fare) ? fare : null;

    public static bool IsLongHaul(string origin, string destination) =>
        LongHaulRoutes.Contains(Key(origin, destination));

    public static int GetTypicalDurationMinutes(string origin, string destination) =>
        TypicalDurationMinutes.TryGetValue(Key(origin, destination), out var minutes) ? minutes : 180;

    public static decimal CabinMultiplier(CabinClass cabinClass) => cabinClass switch
    {
        CabinClass.Economy => 1.0m,
        CabinClass.Business => 2.5m,
        CabinClass.First => 4.0m,
        _ => throw new ArgumentOutOfRangeException(nameof(cabinClass), cabinClass, null)
    };

    // Extracted so the away-from-zero midpoint behaviour (challenge.md "round to 2 decimal
    // places"; docs/02-revision.md's explicit call-out that .NET's default Math.Round is
    // banker's rounding) is directly unit-testable with synthetic values, independent of
    // whether any of the 6 approved airport pairs happens to produce a real midpoint case.
    public static decimal RoundAwayFromZero(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static (string, string) Key(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
}
