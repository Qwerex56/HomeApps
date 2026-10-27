using System.Net;
using System.Net.Http.Json;

namespace Accounts.Api.Tests;

public sealed class AccessTests(AccountsApiFactory api) : IClassFixture<AccountsApiFactory>
{
    [Fact]
    public async Task Without_a_token_everything_but_health_is_401()
    {
        using var client = api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/users/me/reactivation")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }

    [Fact]
    public async Task Plain_user_gets_403_on_admin()
    {
        var id = await api.RegisterAsync();
        using var client = api.SignedIn(id);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/admin/users/{id}/enable", null)).StatusCode);
    }

    [Fact]
    public async Task Ticket_opens_only_reactivation_and_access_token_does_not()
    {
        var id = await api.RegisterAsync();
        using var withTicket = api.ClientWith(api.Ticket(id));
        using var withToken = api.SignedIn(id);

        Assert.Equal(HttpStatusCode.Unauthorized, (await withTicket.GetAsync("/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await withTicket.PutAsJsonAsync("/users/me/profile", AccountsApiFactory.Profile())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await withToken.GetAsync("/users/me/reactivation")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await withToken.PostAsJsonAsync("/users/me/reactivate", AccountsApiFactory.Profile())).StatusCode);
    }

    [Fact]
    public async Task Ticket_with_another_scope_or_token_for_another_audience_is_refused()
    {
        var id = await api.RegisterAsync();
        using var linkTicket = api.ClientWith(api.Ticket(id, scope: "external_link"));
        using var foreignToken = api.ClientWith(api.AccessToken(id, audience: "someone-else"));

        Assert.Equal(HttpStatusCode.Unauthorized, (await linkTicket.GetAsync("/users/me/reactivation")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await foreignToken.GetAsync("/users/me")).StatusCode);
    }

    [Fact]
    public async Task Account_not_created_yet_is_404_user_pending()
    {
        using var client = api.SignedIn(Guid.NewGuid());

        using var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("user.pending", await response.CodeAsync());
    }
}
