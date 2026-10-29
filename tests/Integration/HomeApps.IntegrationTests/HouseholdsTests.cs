using System.Net;
using System.Text.Json;

namespace HomeApps.IntegrationTests;

/// <summary>Households plan step 5: what only works when Households hears Auth's and Accounts' events through RabbitMQ.</summary>
public sealed class HouseholdsTests(HomeAppsHost apps) : IClassFixture<HomeAppsHost>
{
    [Fact]
    public async Task People_who_signed_up_in_auth_join_a_household_and_are_listed_by_their_names()
    {
        var anna = await apps.SignUpAsync("Anna");
        var ewa = await apps.SignUpAsync("Ewa");
        var household = await HouseholdAsync(anna, "Kowalscy");

        await JoinAsync(household, anna, ewa);

        await MembersAsync(household, anna.Tokens.Access, members =>
            members.Any(m => m.GetProperty("name").GetString() == "Anna" && m.GetProperty("role").GetString() == "Owner")
            && members.Any(m => m.GetProperty("name").GetString() == "Ewa" && m.GetProperty("role").GetString() == "Member"),
            "both names to come from Accounts");
    }

    [Fact]
    public async Task Account_disabled_in_accounts_is_greyed_out_in_its_households()
    {
        var anna = await apps.SignUpAsync("Ania");
        var kuba = await apps.SignUpAsync("Kuba");
        var household = await HouseholdAsync(anna, "Nowakowie");
        await JoinAsync(household, anna, kuba);
        await apps.AccountAsync(kuba.Tokens.Access);
        using var accounts = apps.Accounts.CreateClient();

        using var disable = await accounts.SendAsync(HttpMethod.Post, "/users/me/disable", kuba.Tokens.Access, new { reason = "Przerwa" });
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);

        await MembersAsync(household, anna.Tokens.Access, members =>
            members.Any(m => m.GetProperty("userId").GetGuid() == kuba.Id && !m.GetProperty("isActive").GetBoolean()),
            "UserDisabled to reach Households");
    }

    [Fact]
    public async Task Banned_owner_hands_the_household_to_the_oldest_member_who_is_mailed_and_erasing_removes_them()
    {
        var owner = await apps.OwnerAsync();
        var zenek = await apps.SignUpAsync("Zenek");
        var ola = await apps.SignUpAsync("Ola");
        var household = await HouseholdAsync(zenek, "Zenkowie");
        await JoinAsync(household, zenek, ola);
        await apps.AccountAsync(zenek.Tokens.Access);
        using var accounts = apps.Accounts.CreateClient();
        using var households = apps.Households.CreateClient();

        using var disable = await accounts.SendAsync(HttpMethod.Post, $"/admin/users/{zenek.Id}/disable", owner.Access, new { reason = "Spam" });
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);

        var view = await HomeAppsHost.EventuallyAsync<JsonElement?>(async () =>
        {
            var found = await (await households.SendAsync(HttpMethod.Get, $"/households/{household}", ola.Tokens.Access)).ReadJsonAsync();
            return found.GetProperty("myRole").GetString() == "Owner" ? found : null;
        }, "the automatic handover (D5)");
        Assert.NotEqual(JsonValueKind.Null, view!.Value.GetProperty("ownershipReceivedAt").ValueKind);
        var mail = await apps.Emails.SentToAsync(ola.Email, "Zenkowie");
        Assert.Contains("stracił dostęp", mail.Text, StringComparison.Ordinal);
        Assert.Contains($"/households/{household}", mail.Text, StringComparison.Ordinal);

        using var erase = await accounts.SendAsync(HttpMethod.Delete, $"/admin/users/{zenek.Id}", owner.Access, new { confirmEmail = zenek.Email });
        Assert.Equal(HttpStatusCode.NoContent, erase.StatusCode);
        await MembersAsync(household, ola.Tokens.Access, members => members.Count == 1, "UserErased to reach Households");
    }

    private async Task<Guid> HouseholdAsync(HomeAppsHost.Person owner, string name)
    {
        using var households = apps.Households.CreateClient();
        using var created = await households.SendAsync(HttpMethod.Post, "/households", owner.Tokens.Access, new { name });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.ReadJsonAsync()).GetProperty("id").GetGuid();
    }

    private async Task JoinAsync(Guid household, HomeAppsHost.Person by, HomeAppsHost.Person joiner)
    {
        using var households = apps.Households.CreateClient();
        using var invite = await households.SendAsync(HttpMethod.Post, $"/households/{household}/invites", by.Tokens.Access, new { role = "Member" });
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
        var code = (await invite.ReadJsonAsync()).GetProperty("code").GetString();

        using var accepted = await households.SendAsync(HttpMethod.Post, $"/invites/{code}/accept", joiner.Tokens.Access);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    private async Task MembersAsync(Guid household, string accessToken, Func<List<JsonElement>, bool> condition, string what)
    {
        using var households = apps.Households.CreateClient();
        await HomeAppsHost.EventuallyAsync(async () =>
        {
            var members = (await (await households.SendAsync(HttpMethod.Get, $"/households/{household}/members", accessToken)).ReadJsonAsync())
                .EnumerateArray().ToList();
            return condition(members) ? (object)true : null;
        }, what);
    }
}
