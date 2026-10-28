using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Households.Api.Tests;

public sealed class InviteTests(HouseholdsApiFactory api) : IClassFixture<HouseholdsApiFactory>
{
    [Fact]
    public async Task Invite_is_shown_previewed_and_accepted()
    {
        var owner = await api.UserAsync("Anna");
        var household = await api.HouseholdAsync(owner, "Kowalscy");
        using var ownerClient = api.SignedIn(owner);

        using var created = await ownerClient.PostAsJsonAsync($"/households/{household}/invites", new { role = "Member", validity = "OneDay" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var invite = await created.ReadJsonAsync();
        var code = invite.GetProperty("code").GetString()!;
        Assert.Matches("^[0-9A-Z]{5}-[0-9A-Z]{5}$", code);
        Assert.Equal("Anna", invite.GetProperty("createdByName").GetString());

        var joiner = await api.UserAsync();
        using var joinerClient = api.SignedIn(joiner);
        var preview = await joinerClient.GetFromJsonAsync<JsonElement>($"/invites/{code.Replace("-", "", StringComparison.Ordinal).ToLowerInvariant()}");
        Assert.Equal("Kowalscy", preview.GetProperty("householdName").GetString());
        Assert.Equal("Anna", preview.GetProperty("invitedByName").GetString());

        using var accepted = await joinerClient.PostAsync($"/invites/{code}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(household, (await accepted.ReadJsonAsync()).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.Conflict, (await joinerClient.PostAsync($"/invites/{code}/accept", null)).StatusCode);
    }

    [Fact]
    public async Task Invite_cannot_give_more_than_Member_and_a_revoked_invite_is_gone()
    {
        var owner = await api.UserAsync();
        var household = await api.HouseholdAsync(owner);
        using var client = api.SignedIn(owner);

        Assert.NotEqual(HttpStatusCode.Created,
            (await client.PostAsJsonAsync($"/households/{household}/invites", new { role = "Admin" })).StatusCode);

        using var created = await client.PostAsJsonAsync($"/households/{household}/invites", new { role = "Member" });
        var invite = await created.ReadJsonAsync();
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/households/{household}/invites/{invite.GetProperty("id").GetGuid()}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>($"/households/{household}/invites")).EnumerateArray());

        using var joiner = api.SignedIn(await api.UserAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await joiner.GetAsync($"/invites/{invite.GetProperty("code").GetString()}")).StatusCode);
    }

    [Fact]
    public async Task Guest_through_the_guest_link_sees_the_household_but_not_its_members()
    {
        var owner = await api.UserAsync();
        var household = await api.HouseholdAsync(owner);
        using var ownerClient = api.SignedIn(owner);
        using var link = await ownerClient.PostAsync($"/households/{household}/guest-link", null);
        var code = (await link.ReadJsonAsync()).GetProperty("code").GetString()!;

        var guest = await api.UserAsync();
        using var guestClient = api.SignedIn(guest);
        Assert.Equal("Guest", (await guestClient.GetFromJsonAsync<JsonElement>($"/invites/{code}")).GetProperty("role").GetString());
        Assert.Equal(HttpStatusCode.OK, (await guestClient.PostAsync($"/invites/{code}/accept", null)).StatusCode);

        var view = await guestClient.GetFromJsonAsync<JsonElement>($"/households/{household}");
        Assert.Equal("Guest", view.GetProperty("myRole").GetString());
        Assert.Equal(JsonValueKind.Null, view.GetProperty("memberCount").ValueKind);
        Assert.Equal(JsonValueKind.Null, view.GetProperty("guestLink").ValueKind);
        Assert.Equal(HttpStatusCode.Forbidden, (await guestClient.GetAsync($"/households/{household}/members")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await guestClient.PostAsJsonAsync($"/households/{household}/invites", new { role = "Guest" })).StatusCode);

        var until = DateTimeOffset.UtcNow.AddDays(1);
        Assert.Equal(HttpStatusCode.NoContent,
            (await ownerClient.PutAsJsonAsync($"/households/{household}/members/{guest}/access", new { until })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.DeleteAsync($"/households/{household}/guest-link")).StatusCode);
        using var late = api.SignedIn(await api.UserAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await late.GetAsync($"/invites/{code}")).StatusCode);
    }
}
