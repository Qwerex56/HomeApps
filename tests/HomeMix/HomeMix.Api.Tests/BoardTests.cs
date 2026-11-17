using System.Net;
using System.Net.Http.Json;
using HomeApps.Contracts.Households;

namespace HomeMix.Api.Tests;

/// <summary>UI 2.4.1: where a household's widgets stand on its page and its wall tablet.</summary>
public sealed class BoardTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    [Fact]
    public async Task An_admin_arranges_and_every_member_reads_the_board()
    {
        var (admin, member) = (Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Member);
        using var admins = api.SignedIn(admin);
        using var members = api.SignedIn(member);

        var empty = await (await members.GetAsync($"/homemix/api/households/{household}/boards/Household")).ReadJsonAsync();
        using var saved = await admins.PutAsJsonAsync($"/homemix/api/households/{household}/boards/Household", new
        {
            items = new object[]
            {
                new { key = "core:household", x = 0, y = 0, w = 3, h = 2 },
                new { key = "mix:abc", x = 3, y = 0, w = 2, h = 1, fit = "Fill" },
            },
        });
        var read = await (await members.GetAsync($"/homemix/api/households/{household}/boards/Household")).ReadJsonAsync();
        var wall = await (await members.GetAsync($"/homemix/api/households/{household}/boards/Wall")).ReadJsonAsync();

        Assert.Empty(empty.GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var items = read.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(["core:household", "mix:abc"], items.Select(i => i.GetProperty("key").GetString()));
        Assert.Equal("Center", items[0].GetProperty("fit").GetString());
        Assert.Equal("Fill", items[1].GetProperty("fit").GetString());
        Assert.Empty(wall.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Members_may_not_arrange_outsiders_see_nothing_and_overlaps_are_refused()
    {
        var (admin, member) = (Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Member);
        using var admins = api.SignedIn(admin);
        using var members = api.SignedIn(member);
        using var outsider = api.SignedIn(Guid.NewGuid());
        var layout = new { items = new object[] { new { key = "a", x = 0, y = 0, w = 2, h = 1 }, new { key = "b", x = 1, y = 0, w = 1, h = 1 } } };

        using var forbidden = await members.PutAsJsonAsync($"/homemix/api/households/{household}/boards/Household", new { items = Array.Empty<object>() });
        using var hidden = await outsider.GetAsync($"/homemix/api/households/{household}/boards/Household");
        using var place = await admins.GetAsync($"/homemix/api/households/{household}/boards/Account");
        using var overlap = await admins.PutAsJsonAsync($"/homemix/api/households/{household}/boards/Wall", layout);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, place.StatusCode);
        Assert.Equal("homemix.board_overlap", await overlap.CodeAsync());
    }
}
