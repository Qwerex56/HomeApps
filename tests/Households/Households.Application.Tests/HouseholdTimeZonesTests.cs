using Households.Application.Common;

namespace Households.Application.Tests;

/// <summary>2.11: the effective zone is the household's own, else the owner's account zone, else the fallback.</summary>
public sealed class HouseholdTimeZonesTests
{
    [Fact]
    public void The_households_own_zone_wins_over_the_owner()
    {
        Assert.Equal(("Europe/London", "household"), HouseholdTimeZones.Resolve("Europe/London", "Europe/Warsaw"));
    }

    [Fact]
    public void Without_its_own_zone_the_household_follows_the_owner()
    {
        Assert.Equal(("America/New_York", "owner"), HouseholdTimeZones.Resolve(null, "America/New_York"));
        Assert.Equal(("America/New_York", "owner"), HouseholdTimeZones.Resolve("", "America/New_York"));
    }

    [Fact]
    public void With_neither_zone_the_fallback_is_used()
    {
        Assert.Equal((HouseholdTimeZones.Fallback, HouseholdTimeZones.FallbackSource), HouseholdTimeZones.Resolve(null, null));
    }
}
