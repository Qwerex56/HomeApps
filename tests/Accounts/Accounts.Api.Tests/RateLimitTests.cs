using System.Net;
using System.Net.Http.Json;

namespace Accounts.Api.Tests;

public sealed class LowLimitFactory : AccountsApiFactory
{
    protected override int Changes => 2;
}

public sealed class RateLimitTests(LowLimitFactory api) : IClassFixture<LowLimitFactory>
{
    [Fact]
    public async Task Third_change_in_the_window_gets_429_but_reads_and_other_users_go_on()
    {
        var id = await api.RegisterAsync();
        using var client = api.SignedIn(id);

        using var first = await client.PutAsJsonAsync("/users/me/profile", AccountsApiFactory.Profile("Raz"));
        using var second = await client.PutAsJsonAsync("/users/me/profile", AccountsApiFactory.Profile("Dwa"));
        using var third = await client.PutAsJsonAsync("/users/me/profile", AccountsApiFactory.Profile("Trzy"));

        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.NotNull(third.Headers.RetryAfter);
        Assert.Equal("rate_limited", await third.CodeAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/users/me")).StatusCode);

        var someoneElse = await api.RegisterAsync();
        using var other = api.SignedIn(someoneElse);
        Assert.Equal(HttpStatusCode.NoContent, (await other.PutAsJsonAsync("/users/me/profile", AccountsApiFactory.Profile("Inny"))).StatusCode);
    }

    [Fact]
    public async Task Admin_edits_of_other_accounts_count_against_the_admin()
    {
        var a = await api.RegisterAsync();
        var b = await api.RegisterAsync();
        var c = await api.RegisterAsync();
        using var admin = api.SignedIn(Guid.NewGuid(), "Admin");

        await admin.PutAsJsonAsync($"/admin/users/{a}/profile", AccountsApiFactory.Profile("A"));
        await admin.PutAsJsonAsync($"/admin/users/{b}/profile", AccountsApiFactory.Profile("B"));
        using var third = await admin.PutAsJsonAsync($"/admin/users/{c}/profile", AccountsApiFactory.Profile("C"));

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }
}
