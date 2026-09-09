using SkyRoute.Domain.Enums;
using SkyRoute.Domain.Models;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Tests.Domain;

public class FlightSearchCriteriaTests
{
    private static readonly Airport Jfk = new("JFK", "New York", "United States", "US");
    private static readonly Airport Lax = new("LAX", "Los Angeles", "United States", "US");
    private static readonly Airport Lhr = new("LHR", "London", "United Kingdom", "GB");

    [Fact]
    public void IsInternational_IsFalse_WhenOriginAndDestinationShareCountryCode()
    {
        var criteria = new FlightSearchCriteria
        {
            Origin = Jfk,
            Destination = Lax,
            DepartureDate = new DateOnly(2025, 12, 1),
            PassengerCount = 1,
            CabinClass = CabinClass.Economy
        };

        Assert.False(criteria.IsInternational);
    }

    [Fact]
    public void IsInternational_IsTrue_WhenOriginAndDestinationHaveDifferentCountryCodes()
    {
        var criteria = new FlightSearchCriteria
        {
            Origin = Jfk,
            Destination = Lhr,
            DepartureDate = new DateOnly(2025, 12, 1),
            PassengerCount = 1,
            CabinClass = CabinClass.Economy
        };

        Assert.True(criteria.IsInternational);
    }
}
