using System.Net;
using System.Net.Http.Json;
using HomeApps.Contracts.Households;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "Notifications": <c>notify.push</c> reaches only people of the add-on's household.</summary>
public sealed class NotifyTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    private static readonly byte[] Notifying = AddonZip.Create(AddonZip.Manifest(id: "timer", capabilities: """["storage.household", "notify"]"""));

    [Fact]
    public async Task A_member_notifies_everyone_else_or_named_people_of_the_household()
    {
        var (admin, member) = (Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Member);
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(household, Notifying, Requests.Notify);
        using var members = api.SignedIn(member);

        using var everyone = await members.PostAsJsonAsync($"/homemix/api/installations/{id}/notify",
            new { title = "Koniec", body = "Przerwa!", path = "timer", tag = "end" });
        using var named = await members.PostAsJsonAsync($"/homemix/api/installations/{id}/notify",
            new { userIds = new[] { admin, member }, title = "Koniec", body = "Przerwa!" });
        using var stranger = await members.PostAsJsonAsync($"/homemix/api/installations/{id}/notify",
            new { userIds = new[] { Guid.NewGuid() }, title = "Koniec", body = "Przerwa!" });

        Assert.Equal(1, (await everyone.ReadJsonAsync()).GetProperty("recipients").GetInt32());
        Assert.Equal(2, (await named.ReadJsonAsync()).GetProperty("recipients").GetInt32());
        Assert.Equal("homemix.not_a_recipient", await stranger.CodeAsync());
    }

    [Fact]
    public async Task Without_the_capability_or_access_nothing_is_sent()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        var quiet = await admins.InstalledInAsync(household, AddonZip.Create(AddonZip.Manifest(id: "quiet")));
        var loud = await admins.InstalledInAsync(household, AddonZip.Create(AddonZip.Manifest(id: "loud",
            capabilities: """["storage.household", "notify"]""")), Requests.Notify);
        using var outsider = api.SignedIn(Guid.NewGuid());

        using var missing = await admins.PostAsJsonAsync($"/homemix/api/installations/{quiet}/notify", new { title = "a", body = "b" });
        using var foreign = await outsider.PostAsJsonAsync($"/homemix/api/installations/{loud}/notify", new { title = "a", body = "b" });
        using var badPath = await admins.PostAsJsonAsync($"/homemix/api/installations/{loud}/notify", new { title = "a", body = "b", path = "https://evil" });

        Assert.Equal("homemix.capability_missing", await missing.CodeAsync());
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badPath.StatusCode);
    }

    [Fact]
    public async Task On_an_account_only_its_owner_is_notified()
    {
        var me = Guid.NewGuid();
        using var client = api.SignedIn(me);
        var sha = await client.PackageAsync(AddonZip.Create(AddonZip.Manifest(id: "solo", capabilities: """["storage.user", "notify"]""")));
        using var installed = await client.PostAsJsonAsync("/homemix/api/me/installations",
            new { sha256 = sha, consent = Requests.UserNotify });
        var id = (await installed.ReadJsonAsync()).GetProperty("id").GetGuid();

        using var self = await client.PostAsJsonAsync($"/homemix/api/installations/{id}/notify", new { title = "a", body = "b" });
        using var other = await client.PostAsJsonAsync($"/homemix/api/installations/{id}/notify",
            new { userIds = new[] { Guid.NewGuid() }, title = "a", body = "b" });

        Assert.Equal(1, (await self.ReadJsonAsync()).GetProperty("recipients").GetInt32());
        Assert.Equal("homemix.not_a_recipient", await other.CodeAsync());
    }
}
