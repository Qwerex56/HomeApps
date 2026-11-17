using System.Net;
using System.Net.Http.Json;

namespace HomeMix.Api.Tests;

/// <summary>UI 2.4.1: my account's add-ons on the quick links bar, in order, at most 8.</summary>
public sealed class PinTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    private static async Task<Guid> InstallMineAsync(HttpClient client, string id)
    {
        var sha = await client.PackageAsync(AddonZip.Create(AddonZip.Manifest(id: id)));
        using var install = await client.PostAsJsonAsync("/homemix/api/me/installations", new { sha256 = sha, consent = Requests.Household });
        return (await install.ReadJsonAsync()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task I_pin_my_addons_in_order_and_unpin_the_rest()
    {
        using var client = api.SignedIn(Guid.NewGuid());
        var (a, b, c) = (await InstallMineAsync(client, "pina"), await InstallMineAsync(client, "pinb"), await InstallMineAsync(client, "pinc"));
        (await client.PutAsJsonAsync("/homemix/api/me/pins", new { installations = new[] { a, b } })).Dispose();

        using var pinned = await client.PutAsJsonAsync("/homemix/api/me/pins", new { installations = new[] { c, a } });
        var mine = (await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/homemix/api/me/installations"))
            .EnumerateArray().ToDictionary(i => i.GetProperty("id").GetGuid(), i => i.GetProperty("pin"));

        Assert.Equal(HttpStatusCode.OK, pinned.StatusCode);
        Assert.Equal(1, mine[a].GetInt32());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, mine[b].ValueKind);
        Assert.Equal(0, mine[c].GetInt32());
    }

    [Fact]
    public async Task Someone_elses_or_a_household_addon_cannot_be_pinned_and_nine_are_too_many()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var client = api.SignedIn(admin);
        var inHousehold = await client.InstalledInAsync(household, AddonZip.Create(AddonZip.Manifest(id: "pinhome")), Requests.Household);

        using var foreign = await client.PutAsJsonAsync("/homemix/api/me/pins", new { installations = new[] { inHousehold } });
        using var nine = await client.PutAsJsonAsync("/homemix/api/me/pins", new { installations = Enumerable.Range(0, 9).Select(_ => Guid.NewGuid()) });

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, nine.StatusCode);
    }
}
