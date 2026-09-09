using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Tests.Infrastructure;

// Proves the away-from-zero midpoint behaviour required by challenge.md ("round to 2 decimal
// places") and called out explicitly in docs/02-revision.md: .NET's default Math.Round is
// banker's rounding (2.345 -> 2.34), which is NOT what the brief means. Tested here with
// synthetic values because none of the 6 approved RouteFareTable airport pairs, combined with
// the three cabin multipliers, happens to produce a true midpoint case end-to-end.
public class RouteFareTableRoundingTests
{
    [Theory]
    [InlineData(2.345, 2.35)]  // canonical midpoint: banker's would give 2.34, away-from-zero gives 2.35
    [InlineData(2.355, 2.36)]  // banker's would give 2.36 too here, so also assert the odd-preceding-digit case below
    [InlineData(1.005, 1.01)]  // banker's would give 1.00 (round-to-even), away-from-zero gives 1.01
    public void RoundAwayFromZero_RoundsMidpointsUp_RegardlessOfPrecedingDigitParity(decimal value, decimal expected)
    {
        Assert.Equal(expected, RouteFareTable.RoundAwayFromZero(value));
    }

    [Fact]
    public void RoundAwayFromZero_DiffersFromBankersRounding_OnEvenPrecedingDigitMidpoint()
    {
        // 2.345 rounded to 2 decimals: banker's rounding (Math.Round default) rounds the
        // midpoint to the nearest EVEN digit -> 2.34. Away-from-zero always rounds up -> 2.35.
        var bankersResult = Math.Round(2.345m, 2, MidpointRounding.ToEven);
        var awayFromZeroResult = RouteFareTable.RoundAwayFromZero(2.345m);

        Assert.Equal(2.34m, bankersResult);
        Assert.Equal(2.35m, awayFromZeroResult);
        Assert.NotEqual(bankersResult, awayFromZeroResult);
    }

    [Fact]
    public void RoundAwayFromZero_RoundsNonMidpointValues_Normally()
    {
        Assert.Equal(207.00m, RouteFareTable.RoundAwayFromZero(207.00m));
        Assert.Equal(34.50m, RouteFareTable.RoundAwayFromZero(34.4999m));
    }
}
