using System.Net;
using System.Net.Http.Json;
using HomeApps.Contracts.Households;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "Installing": admins and up manage a household's add-ons, members use them, an account's are its owner's.</summary>
public sealed class InstallationTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    [Fact]
    public async Task An_admin_installs_and_every_member_sees_it()
    {
        var admin = Guid.NewGuid();
        var member = Guid.NewGuid();
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Guest);
        using var admins = api.SignedIn(admin);

        using var install = await admins.InstallInAsync(household, await admins.PackageAsync(AddonZip.Pomodoro()));
        using var members = api.SignedIn(member);
        var list = await members.GetFromJsonAsync<System.Text.Json.JsonElement>($"/homemix/api/households/{household}/installations");

        Assert.Equal(HttpStatusCode.Created, install.StatusCode);
        var installed = await install.ReadJsonAsync();
        var id = installed.GetProperty("id").GetGuid();
        Assert.Equal($"/homemix/api/installations/{id}", install.Headers.Location!.OriginalString);
        Assert.Equal($"/homemix/run/{id}/", installed.GetProperty("runUrl").GetString());
        Assert.Equal("Household", installed.GetProperty("targetKind").GetString());
        var only = Assert.Single(list.EnumerateArray());
        Assert.Equal(id, only.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task A_member_does_not_install()
    {
        var admin = Guid.NewGuid();
        var member = Guid.NewGuid();
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Member);
        using var members = api.SignedIn(member);

        using var response = await members.InstallInAsync(household, await members.PackageAsync(AddonZip.Pomodoro()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("homemix.forbidden", await response.CodeAsync());
    }

    [Fact]
    public async Task Someone_outside_the_household_sees_nothing()
    {
        var household = api.Household(Guid.NewGuid());
        using var stranger = api.SignedIn(Guid.NewGuid());

        using var response = await stranger.GetAsync($"/homemix/api/households/{household}/installations");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Installing_needs_consent_to_everything_asked()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        var sha = await admins.PackageAsync(AddonZip.Pomodoro(version: "1.5.0", network: """["https://api.open-meteo.com"]"""));

        using var refused = await admins.InstallInAsync(household, sha);
        using var accepted = await admins.InstallInAsync(household, sha,
            Requests.OpenMeteo);

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("homemix.consent_required", await refused.CodeAsync());
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
    }

    [Fact]
    public async Task One_addon_is_installed_once_per_place()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        await admins.InstalledInAsync(household, AddonZip.Pomodoro());

        using var again = await admins.InstallInAsync(household, await admins.PackageAsync(AddonZip.Pomodoro(version: "1.0.2")));

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("homemix.already_installed", await again.CodeAsync());
    }

    [Fact]
    public async Task A_person_installs_on_their_own_account()
    {
        var adam = Guid.NewGuid();
        using var client = api.SignedIn(adam);
        var sha = await client.PackageAsync(AddonZip.Pomodoro());

        using var install = await client.PostAsJsonAsync("/homemix/api/me/installations", new { sha256 = sha, consent = Requests.Household });
        var mine = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/homemix/api/me/installations");
        using var others = api.SignedIn(Guid.NewGuid());
        var id = (await install.ReadJsonAsync()).GetProperty("id").GetGuid();
        using var foreign = await others.GetAsync($"/homemix/api/installations/{id}");

        Assert.Equal(HttpStatusCode.Created, install.StatusCode);
        Assert.Equal(adam, Assert.Single(mine.EnumerateArray()).GetProperty("targetId").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
    }

    [Fact]
    public async Task A_household_only_addon_is_not_installed_on_an_account()
    {
        using var client = api.SignedIn(Guid.NewGuid());
        var sha = await client.PackageAsync(AddonZip.Create(AddonZip.Manifest(id: "chores", targets: """["Household"]""")));

        using var install = await client.PostAsJsonAsync("/homemix/api/me/installations", new { sha256 = sha, consent = Requests.Household });

        Assert.Equal(HttpStatusCode.BadRequest, install.StatusCode);
        Assert.Equal("homemix.target_not_supported", await install.CodeAsync());
    }

    [Fact]
    public async Task An_update_asking_for_more_needs_consent_again()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(household, AddonZip.Pomodoro(version: "1.0.3"));
        var same = await admins.PackageAsync(AddonZip.Pomodoro(version: "1.1.0"));
        var more = await admins.PackageAsync(AddonZip.Pomodoro(version: "2.0.0", capabilities: """["storage.household", "notify"]"""));

        using var quiet = await admins.PutAsJsonAsync($"/homemix/api/installations/{id}", new { sha256 = same });
        using var refused = await admins.PutAsJsonAsync($"/homemix/api/installations/{id}", new { sha256 = more });
        using var accepted = await admins.PutAsJsonAsync($"/homemix/api/installations/{id}",
            new { sha256 = more, consent = Requests.Notify });

        Assert.Equal(HttpStatusCode.OK, quiet.StatusCode);
        Assert.Equal("1.1.0", (await quiet.ReadJsonAsync()).GetProperty("version").GetString());
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("homemix.consent_required", await refused.CodeAsync());
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal("2.0.0", (await accepted.ReadJsonAsync()).GetProperty("version").GetString());
    }

    [Fact]
    public async Task Removing_takes_it_off_the_list()
    {
        var admin = Guid.NewGuid();
        var member = Guid.NewGuid();
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Member);
        using var admins = api.SignedIn(admin);
        using var members = api.SignedIn(member);
        var id = await admins.InstalledInAsync(household, AddonZip.Pomodoro());

        using var byMember = await members.DeleteAsync($"/homemix/api/installations/{id}");
        using var byAdmin = await admins.DeleteAsync($"/homemix/api/installations/{id}");
        var list = await admins.GetFromJsonAsync<System.Text.Json.JsonElement>($"/homemix/api/households/{household}/installations");

        Assert.Equal(HttpStatusCode.Forbidden, byMember.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);
        Assert.Empty(list.EnumerateArray());
    }
}
