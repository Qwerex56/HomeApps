using System.Security.Claims;
using HomeApps.BuildingBlocks.Api.Auth;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace HomeApps.BuildingBlocks.Tests;

public sealed class HttpContextCurrentUserTests
{
    private static readonly Guid Household = Guid.NewGuid();

    private static HttpContextCurrentUser For(params Claim[] claims) => For(authenticated: true, claims);

    private static HttpContextCurrentUser For(bool authenticated, params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(authenticated ? new ClaimsIdentity(claims, "test") : new ClaimsIdentity(claims)),
        };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        return new HttpContextCurrentUser(accessor);
    }

    private static Claim Sub() => new(ClaimNames.Subject, Guid.NewGuid().ToString());

    [Fact]
    public void A_satellite_token_names_its_household()
    {
        var user = For(Sub(), new Claim(ClaimNames.Actor, """{"sub":"device"}"""), new Claim(ClaimNames.Household, Household.ToString()));

        Assert.Equal(Household, user.SatelliteHouseholdId);
    }

    [Fact]
    public void A_satellite_token_delegated_to_a_service_still_names_its_household()
    {
        // Auth issues act = the service's client id when a device token is delegated (TokenIssuer.IssueDeviceToken).
        var user = For(Sub(), new Claim(ClaimNames.Actor, """{"sub":"jarvis"}"""), new Claim(ClaimNames.Household, Household.ToString()));

        Assert.Equal(Household, user.SatelliteHouseholdId);
    }

    [Fact]
    public void A_normal_user_token_is_no_satellite()
    {
        var user = For(Sub(), new Claim(ClaimNames.Role, "User"));

        Assert.Null(user.SatelliteHouseholdId);
    }

    [Fact]
    public void A_delegated_token_of_a_person_is_no_satellite()
    {
        var user = For(Sub(), new Claim(ClaimNames.Actor, """{"sub":"jarvis"}"""));

        Assert.Null(user.SatelliteHouseholdId);
    }

    [Fact]
    public void A_household_claim_without_act_is_no_satellite()
    {
        var user = For(Sub(), new Claim(ClaimNames.Household, Household.ToString()));

        Assert.Null(user.SatelliteHouseholdId);
    }

    [Fact]
    public void A_malformed_household_claim_is_no_satellite()
    {
        var user = For(Sub(), new Claim(ClaimNames.Actor, """{"sub":"device"}"""), new Claim(ClaimNames.Household, "not-a-guid"));

        Assert.Null(user.SatelliteHouseholdId);
    }

    [Fact]
    public void An_unauthenticated_caller_is_no_satellite()
    {
        var user = For(authenticated: false, new Claim(ClaimNames.Actor, """{"sub":"device"}"""), new Claim(ClaimNames.Household, Household.ToString()));

        Assert.Null(user.SatelliteHouseholdId);
    }
}
