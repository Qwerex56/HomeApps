using System.Net;
using System.Net.Http.Json;

namespace Accounts.Api.Tests;

public sealed class MyAccountTests(AccountsApiFactory api) : IClassFixture<AccountsApiFactory>
{
    [Fact]
    public async Task Read_edit_and_history()
    {
        var email = AccountsApiFactory.Unique("ola");
        var id = await api.RegisterAsync(email, "Ola");
        using var client = api.SignedIn(id);

        var me = await (await client.GetAsync("/users/me")).ReadJsonAsync();
        Assert.Equal(email, me.GetProperty("email").GetString());
        Assert.Equal("User", me.GetProperty("role").GetString());
        Assert.Equal("Active", me.GetProperty("status").GetString());
        Assert.Equal("Password", me.GetProperty("registration").GetProperty("source").GetString());
        Assert.False(me.TryGetProperty("lastSeenAt", out _));

        using var put = await client.PutAsJsonAsync("/users/me/profile", AccountsApiFactory.Profile("Ola Nowa"));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var profile = (await (await client.GetAsync("/users/me")).ReadJsonAsync()).GetProperty("profile");
        Assert.Equal("Ola Nowa", profile.GetProperty("displayName").GetString());
        Assert.Equal("1990-05-01", profile.GetProperty("dateOfBirth").GetString());

        var history = await (await client.GetAsync("/users/me/history")).ReadJsonAsync();
        Assert.Equal(["ProfileUpdated", "Registered"], history.EnumerateArray().Select(e => e.GetProperty("action").GetString()));
        Assert.False(history[0].TryGetProperty("actorId", out _));
    }

    [Fact]
    public async Task Bad_profile_lists_every_bad_field()
    {
        var id = await api.RegisterAsync();
        using var client = api.SignedIn(id);

        using var response = await client.PutAsJsonAsync("/users/me/profile", new
        {
            displayName = "A",
            locale = "pl-PL",
            timeZone = "Mars/Olympus",
            avatarUrl = "http://not-https.example.com/a.png",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("profile.display_name_invalid", body, StringComparison.Ordinal);
        Assert.Contains("profile.time_zone_invalid", body, StringComparison.Ordinal);
        Assert.Contains("profile.avatar_url_invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Disable_then_reactivate_with_the_ticket()
    {
        var id = await api.RegisterAsync(displayName: "Kuba");
        using var client = api.SignedIn(id);
        using var ticket = api.ClientWith(api.Ticket(id));

        using var disable = await client.PostAsJsonAsync("/users/me/disable", new { reason = "Przerwa" });
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);
        var disabled = await (await client.GetAsync("/users/me")).ReadJsonAsync();
        Assert.Equal("Disabled", disabled.GetProperty("status").GetString());
        Assert.True(disabled.GetProperty("disabling").GetProperty("bySelf").GetBoolean());
        Assert.False(disabled.GetProperty("disabling").TryGetProperty("byUserId", out var by) && by.ValueKind != System.Text.Json.JsonValueKind.Null);

        var form = await (await ticket.GetAsync("/users/me/reactivation")).ReadJsonAsync();
        Assert.Equal("Kuba", form.GetProperty("displayName").GetString());

        using var reactivate = await ticket.PostAsJsonAsync("/users/me/reactivate", AccountsApiFactory.Profile("Kuba"));
        Assert.Equal(HttpStatusCode.NoContent, reactivate.StatusCode);
        Assert.Equal("Active", (await (await client.GetAsync("/users/me")).ReadJsonAsync()).GetProperty("status").GetString());
    }
}
