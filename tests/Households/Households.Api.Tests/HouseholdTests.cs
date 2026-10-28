using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Households.Api.Tests;

public sealed class HouseholdTests(HouseholdsApiFactory api) : IClassFixture<HouseholdsApiFactory>
{
    [Fact]
    public async Task Create_returns_201_with_location_and_shows_in_my_list_with_my_role()
    {
        var owner = await api.UserAsync();
        using var client = api.SignedIn(owner);

        using var created = await client.PostAsJsonAsync("/households", new { name = "Kowalscy", description = "Nasz dom" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.ReadJsonAsync()).GetProperty("id").GetGuid();
        Assert.Equal($"/households/{id}", created.Headers.Location!.OriginalString);
        var mine = (await client.GetFromJsonAsync<JsonElement>("/households")).EnumerateArray().Single();
        Assert.Equal("Kowalscy", mine.GetProperty("name").GetString());
        Assert.Equal("Owner", mine.GetProperty("myRole").GetString());
    }

    [Fact]
    public async Task Owner_updates_and_archive_hides_the_household_until_a_server_admin_restores_it()
    {
        var owner = await api.UserAsync();
        var household = await api.HouseholdAsync(owner);
        using var client = api.SignedIn(owner);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsJsonAsync($"/households/{household}", new { name = "Nowa nazwa", description = (string?)null })).StatusCode);
        Assert.Equal("Nowa nazwa", (await client.GetFromJsonAsync<JsonElement>($"/households/{household}")).GetProperty("name").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/households/{household}/archive", null)).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/households")).EnumerateArray());
        Assert.NotEqual(HttpStatusCode.NoContent, (await client.PostAsync($"/households/{household}/restore", null)).StatusCode);

        using var admin = api.SignedIn(Guid.NewGuid(), "Admin");
        var listed = (await admin.GetFromJsonAsync<JsonElement>("/admin/households?q=nowa")).EnumerateArray()
            .Single(h => h.GetProperty("id").GetGuid() == household);
        Assert.Equal("Archived", listed.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/households/{household}/restore", null)).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<JsonElement>("/households")).EnumerateArray());
    }

    [Fact]
    public async Task Owner_hands_over_and_then_may_leave()
    {
        var owner = await api.UserAsync();
        var household = await api.HouseholdAsync(owner);
        var member = await api.MemberAsync(household, owner);
        using var ownerClient = api.SignedIn(owner);
        using var memberClient = api.SignedIn(member);

        Assert.Equal(HttpStatusCode.Conflict, (await ownerClient.DeleteAsync($"/households/{household}/members/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await ownerClient.PostAsJsonAsync($"/households/{household}/ownership", new { userId = member })).StatusCode);

        // A handover the new Owner can see coming shows no banner; D5a's banner is for ban and erase only.
        var view = await memberClient.GetFromJsonAsync<JsonElement>($"/households/{household}");
        Assert.Equal("Owner", view.GetProperty("myRole").GetString());
        Assert.Equal(JsonValueKind.Null, view.GetProperty("ownershipReceivedAt").ValueKind);
        Assert.Equal(HttpStatusCode.NoContent, (await memberClient.PostAsync($"/households/{household}/ownership/acknowledge", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.DeleteAsync($"/households/{household}/members/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/households/{household}")).StatusCode);
    }

    [Fact]
    public async Task Banned_owner_hands_over_automatically_and_the_successor_sees_the_banner_until_acknowledged()
    {
        var owner = await api.UserAsync();
        var household = await api.HouseholdAsync(owner);
        var member = await api.MemberAsync(household, owner);
        await api.SendAsync(new Application.AccountsSync.DisableUserCopyCommand(owner, BySelf: false));
        using var client = api.SignedIn(member);

        var view = await client.GetFromJsonAsync<JsonElement>($"/households/{household}");
        Assert.Equal("Owner", view.GetProperty("myRole").GetString());
        Assert.NotEqual(JsonValueKind.Null, view.GetProperty("ownershipReceivedAt").ValueKind);
        Assert.True((await client.GetFromJsonAsync<JsonElement>("/households")).EnumerateArray().Single().GetProperty("ownershipReceived").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/households/{household}/ownership/acknowledge", null)).StatusCode);
        Assert.Equal(JsonValueKind.Null,
            (await client.GetFromJsonAsync<JsonElement>($"/households/{household}")).GetProperty("ownershipReceivedAt").ValueKind);
    }
}
