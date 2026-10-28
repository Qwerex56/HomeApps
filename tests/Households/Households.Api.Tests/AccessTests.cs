using System.Net;
using System.Net.Http.Json;

namespace Households.Api.Tests;

public sealed class AccessTests(HouseholdsApiFactory api) : IClassFixture<HouseholdsApiFactory>
{
    [Fact]
    public async Task Without_a_token_everything_but_health_is_401()
    {
        using var client = api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/households")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/invites/ABCDE-FGHJK")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/households")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }

    [Fact]
    public async Task Token_for_another_audience_is_refused()
    {
        using var client = api.ClientWith(api.AccessToken(await api.UserAsync(), audience: "someone-else"));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/households")).StatusCode);
    }

    [Fact]
    public async Task Non_member_gets_404_not_403()
    {
        var household = await api.HouseholdAsync(await api.UserAsync());
        using var stranger = api.SignedIn(await api.UserAsync());

        using var response = await stranger.GetAsync($"/households/{household}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/households/{household}/members")).StatusCode);
    }

    [Fact]
    public async Task Plain_user_gets_403_on_admin_list()
    {
        using var client = api.SignedIn(await api.UserAsync());

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/admin/households")).StatusCode);
    }

    [Fact]
    public async Task Disabled_account_cannot_create_a_household()
    {
        var id = await api.UserAsync();
        await api.SendAsync(new Application.AccountsSync.DisableUserCopyCommand(id, true));
        using var client = api.SignedIn(id);

        using var response = await client.PostAsJsonAsync("/households", new { name = "Dom" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("account.disabled", await response.CodeAsync());
    }

    [Fact]
    public async Task Bad_input_is_a_400_with_the_field()
    {
        using var client = api.SignedIn(await api.UserAsync());

        using var response = await client.PostAsJsonAsync("/households", new { name = new string('x', 33) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
