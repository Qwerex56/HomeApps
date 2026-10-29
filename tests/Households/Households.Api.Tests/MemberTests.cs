using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Households.Api.Tests;

public sealed class MemberTests(HouseholdsApiFactory api) : IClassFixture<HouseholdsApiFactory>
{
    [Fact]
    public async Task Members_list_has_names_roles_and_greys_out_disabled_accounts()
    {
        var owner = await api.UserAsync("Anna");
        var household = await api.HouseholdAsync(owner);
        var member = await api.MemberAsync(household, owner, displayName: "Ewa");
        await api.SendAsync(new Application.AccountsSync.DisableUserCopyCommand(member, true));
        using var client = api.SignedIn(owner);

        var members = (await client.GetFromJsonAsync<JsonElement>($"/households/{household}/members")).EnumerateArray().ToList();

        Assert.Equal(2, members.Count);
        var ewa = members.Single(m => m.GetProperty("userId").GetGuid() == member);
        Assert.Equal("Ewa", ewa.GetProperty("name").GetString());
        Assert.Equal("Member", ewa.GetProperty("role").GetString());
        Assert.False(ewa.GetProperty("isActive").GetBoolean());
        Assert.Equal(owner, ewa.GetProperty("invitedBy").GetGuid());
    }

    [Fact]
    public async Task Nickname_role_and_removal()
    {
        var owner = await api.UserAsync();
        var household = await api.HouseholdAsync(owner);
        var member = await api.MemberAsync(household, owner);
        using var ownerClient = api.SignedIn(owner);
        using var memberClient = api.SignedIn(member);

        Assert.Equal(HttpStatusCode.NoContent,
            (await memberClient.PutAsJsonAsync($"/households/{household}/members/me/nickname", new { nickname = "Mama" })).StatusCode);
        var me = await memberClient.GetFromJsonAsync<JsonElement>($"/households/{household}/members/me");
        Assert.Equal("Mama", me.GetProperty("nickname").GetString());

        Assert.Equal(HttpStatusCode.Forbidden,
            (await memberClient.PutAsJsonAsync($"/households/{household}/members/{owner}/role", new { role = "Member" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await ownerClient.PutAsJsonAsync($"/households/{household}/members/{member}/role", new { role = "Admin" })).StatusCode);
        Assert.Equal("Admin", (await memberClient.GetFromJsonAsync<JsonElement>($"/households/{household}/members/me")).GetProperty("role").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.DeleteAsync($"/households/{household}/members/{member}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await memberClient.GetAsync($"/households/{household}")).StatusCode);
    }

    [Fact]
    public async Task Unknown_role_is_400()
    {
        var owner = await api.UserAsync();
        var household = await api.HouseholdAsync(owner);
        var member = await api.MemberAsync(household, owner);
        using var client = api.SignedIn(owner);

        using var response = await client.PutAsJsonAsync($"/households/{household}/members/{member}/role", new { role = "King" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("request.invalid", await response.CodeAsync());
    }
}
