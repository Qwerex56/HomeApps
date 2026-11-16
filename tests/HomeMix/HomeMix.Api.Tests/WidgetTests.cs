using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.Contracts.Households;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "Widgets": which widget stands where, in which size, chosen by admins (B1).</summary>
public sealed class WidgetTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    private const string Widgets = """
        "widgets": [
          { "id": "timer", "title": { "pl": "Minutnik" }, "size": "Small", "sizes": ["Small", "Medium"], "places": ["Household", "Wall", "Account"] },
          { "id": "stats", "title": { "pl": "Statystyki" }, "size": "Large", "places": ["Household"] }
        ]
        """;

    private static byte[] Zip(string version, string widgets = Widgets) => AddonZip.Create(AddonZip.Manifest(version: version, extra: widgets));

    [Fact]
    public async Task An_admin_chooses_the_widget_and_its_size_for_each_place()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(household, Zip("1.0.0"), Requests.Household);

        using var chosen = await admins.PutAsJsonAsync($"/homemix/api/installations/{id}/widgets/Household", new { widget = "timer", size = "Medium", order = 2 });
        using var off = await admins.PutAsJsonAsync($"/homemix/api/installations/{id}/widgets/Wall", new { widget = (string?)null });
        var view = await (await admins.GetAsync($"/homemix/api/installations/{id}")).ReadJsonAsync();

        Assert.Equal(HttpStatusCode.OK, chosen.StatusCode);
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        var choices = view.GetProperty("widgetChoices").EnumerateArray().ToDictionary(c => c.GetProperty("place").GetString()!);
        Assert.Equal("timer", choices["Household"].GetProperty("widgetId").GetString());
        Assert.Equal("Medium", choices["Household"].GetProperty("size").GetString());
        Assert.Equal(2, choices["Household"].GetProperty("order").GetInt32());
        Assert.Equal(JsonValueKind.Null, choices["Wall"].GetProperty("widgetId").ValueKind);
        var timer = view.GetProperty("widgets").EnumerateArray().First(w => w.GetProperty("id").GetString() == "timer");
        Assert.Equal(["Small", "Medium"], timer.GetProperty("sizes").EnumerateArray().Select(s => s.GetString()));
    }

    [Fact]
    public async Task Wrong_choices_are_refused_and_members_may_not_choose()
    {
        var (admin, member) = (Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Member);
        using var admins = api.SignedIn(admin);
        using var members = api.SignedIn(member);
        var id = await admins.InstalledInAsync(household, Zip("1.0.1"), Requests.Household);

        using var size = await admins.PutAsJsonAsync($"/homemix/api/installations/{id}/widgets/Household", new { widget = "timer", size = "Large" });
        using var place = await admins.PutAsJsonAsync($"/homemix/api/installations/{id}/widgets/Wall", new { widget = "stats" });
        using var account = await admins.PutAsJsonAsync($"/homemix/api/installations/{id}/widgets/Account", new { widget = "timer" });
        using var forbidden = await members.PutAsJsonAsync($"/homemix/api/installations/{id}/widgets/Household", new { widget = "timer" });

        Assert.Equal("homemix.widget_size", await size.CodeAsync());
        Assert.Equal("homemix.widget_unknown", await place.CodeAsync());
        Assert.Equal("homemix.widget_place", await account.CodeAsync());
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Reset_goes_back_to_the_default_and_an_update_drops_a_widget_that_is_gone()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(household, Zip("1.0.2"), Requests.Household);
        (await admins.PutAsJsonAsync($"/homemix/api/installations/{id}/widgets/Household", new { widget = "stats" })).Dispose();
        (await admins.PutAsJsonAsync($"/homemix/api/installations/{id}/widgets/Wall", new { widget = "timer", size = "Medium" })).Dispose();

        using var reset = await admins.DeleteAsync($"/homemix/api/installations/{id}/widgets/Wall");
        var sha = await admins.PackageAsync(Zip("1.0.3", """
            "widgets": [{ "id": "timer", "title": { "pl": "Minutnik" }, "size": "Small", "places": ["Household", "Wall"] }]
            """));
        using var updated = await admins.PutAsJsonAsync($"/homemix/api/installations/{id}", new { sha256 = sha });

        Assert.DoesNotContain((await reset.ReadJsonAsync()).GetProperty("widgetChoices").EnumerateArray(),
            c => c.GetProperty("place").GetString() == "Wall");
        Assert.Empty((await updated.ReadJsonAsync()).GetProperty("widgetChoices").EnumerateArray());
    }
}
