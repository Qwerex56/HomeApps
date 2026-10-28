using System.Net;
using System.Net.Http.Json;

namespace Households.Api.Tests;

public sealed class LowLimitFactory : HouseholdsApiFactory
{
    protected override int Invites => 2;

    protected override int Changes => 3;

    protected override int MaxMembers => 2;
}

public sealed class LimitTests(LowLimitFactory api) : IClassFixture<LowLimitFactory>
{
    [Fact]
    public async Task Third_invite_lookup_in_a_minute_gets_429()
    {
        using var client = api.SignedIn(await api.UserAsync());

        using var first = await client.GetAsync("/invites/ABCDE-FGHJK");
        using var second = await client.GetAsync("/invites/ABCDE-FGHJK");
        using var third = await client.GetAsync("/invites/ABCDE-FGHJK");

        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.NotNull(third.Headers.RetryAfter);
        Assert.Equal("rate_limited", await third.CodeAsync());
    }

    [Fact]
    public async Task Fourth_change_gets_429_but_reads_go_on()
    {
        using var client = api.SignedIn(await api.UserAsync());

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/households", new { name = $"Dom {i}" })).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/households", new { name = "Dom 4" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/households")).StatusCode);
    }

    [Fact]
    public async Task Member_limit_comes_from_configuration()
    {
        var owner = await api.UserAsync();
        var household = await api.HouseholdAsync(owner);
        using var ownerClient = api.SignedIn(owner);
        using var created = await ownerClient.PostAsJsonAsync($"/households/{household}/invites", new { role = "Member" });
        var code = (await created.ReadJsonAsync()).GetProperty("code").GetString();

        using var second = api.SignedIn(await api.UserAsync());
        using var third = api.SignedIn(await api.UserAsync());
        Assert.Equal(HttpStatusCode.OK, (await second.PostAsync($"/invites/{code}/accept", null)).StatusCode);
        using var full = await third.PostAsync($"/invites/{code}/accept", null);

        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        Assert.Equal("household.full", await full.CodeAsync());
    }
}
