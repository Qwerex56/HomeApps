using System.Net;
using System.Net.Http.Json;
using HomeApps.Contracts.Households;

namespace HomeMix.Api.Tests;

/// <summary>Release 3.0: the API lives under /v1; the old paths keep answering through 3.x, marked deprecated.</summary>
public sealed class VersioningTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    [Fact]
    public async Task The_old_and_the_new_path_answer_the_same_and_only_the_old_one_is_deprecated()
    {
        var member = Guid.NewGuid();
        var household = api.Household(member);
        api.Households.Set(household, member, HouseholdRole.Member);
        using var client = api.SignedIn(member);

        using var current = await client.GetAsync(new Uri($"/homemix/api/v1/households/{household}/installations", UriKind.Relative));
        using var legacy = await client.GetAsync(new Uri($"/homemix/api/households/{household}/installations", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        Assert.Equal(await current.Content.ReadAsStringAsync(), await legacy.Content.ReadAsStringAsync());
        Assert.False(current.Headers.Contains("Deprecation"));
        Assert.Equal("true", Assert.Single(legacy.Headers.GetValues("Deprecation")));
        Assert.Equal("Sat, 01 Jan 2028 00:00:00 GMT", Assert.Single(legacy.Headers.GetValues("Sunset")));
    }

    [Fact]
    public async Task The_sandbox_stays_without_a_version()
    {
        using var client = api.CreateClient();

        using var versioned = await client.GetAsync(new Uri($"/homemix/run/v1/{Guid.NewGuid()}/", UriKind.Relative));

        Assert.NotEqual(HttpStatusCode.OK, versioned.StatusCode);
        Assert.False(versioned.Headers.Contains("Deprecation"));
    }
}
