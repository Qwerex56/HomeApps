using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Households.Application.AccountsSync;

namespace Households.Api.Tests;

/// <summary>2.11: the household has its own zone, follows the owner's zone until it is set, and only Admin and Owner may change it.</summary>
public sealed class HouseholdTimeZoneTests(HouseholdsApiFactory api) : IClassFixture<HouseholdsApiFactory>
{
    [Fact]
    public async Task Without_its_own_zone_the_household_follows_the_owner_and_an_admin_can_change_it()
    {
        var owner = await api.UserAsync();
        await api.SendAsync(new UpdateUserCopyCommand(owner, "Adam", null, "Europe/Warsaw"));
        var household = await api.HouseholdAsync(owner);
        var admin = await api.MemberAsync(household, owner);

        using (var ownerClient = api.SignedIn(owner))
        {
            Assert.Equal(HttpStatusCode.NoContent,
                (await ownerClient.PutAsJsonAsync($"/households/{household}/members/{admin}/role", new { role = "Admin" })).StatusCode);
            var before = await ownerClient.GetFromJsonAsync<JsonElement>($"/households/{household}");
            Assert.Equal("Europe/Warsaw", before.GetProperty("timeZone").GetString());
            Assert.Equal("owner", before.GetProperty("timeZoneSource").GetString());
        }

        using (var adminClient = api.SignedIn(admin))
        {
            var set = await adminClient.PutAsJsonAsync($"/households/v1/{household}/timezone", new { timeZone = "Europe/London" });
            Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);
        }

        using var check = api.SignedIn(owner);
        var after = await check.GetFromJsonAsync<JsonElement>($"/households/{household}");
        Assert.Equal("Europe/London", after.GetProperty("timeZone").GetString());
        Assert.Equal("household", after.GetProperty("timeZoneSource").GetString());
    }

    [Fact]
    public async Task Members_and_guests_cannot_change_the_zone_and_an_unknown_zone_is_refused()
    {
        var owner = await api.UserAsync();
        var household = await api.HouseholdAsync(owner);
        var member = await api.MemberAsync(household, owner, "Member");
        var guest = await api.MemberAsync(household, owner, "Guest");
        var body = new { timeZone = "Europe/Warsaw" };

        using (var memberClient = api.SignedIn(member))
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.PutAsJsonAsync($"/households/v1/{household}/timezone", body)).StatusCode);
        }

        using (var guestClient = api.SignedIn(guest))
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await guestClient.PutAsJsonAsync($"/households/v1/{household}/timezone", body)).StatusCode);
        }

        using var ownerClient = api.SignedIn(owner);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await ownerClient.PutAsJsonAsync($"/households/v1/{household}/timezone", new { timeZone = "Mars/Base" })).StatusCode);
        var view = await ownerClient.GetFromJsonAsync<JsonElement>($"/households/{household}");
        Assert.NotEqual("household", view.GetProperty("timeZoneSource").GetString());
    }
}
