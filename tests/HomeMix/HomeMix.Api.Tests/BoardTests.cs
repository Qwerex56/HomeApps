using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Households;
using HomeMix.Domain.Boards;
using HomeMix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task A_save_of_an_arranged_board_needs_its_date_or_force_else_409()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        var url = $"/homemix/api/households/{household}/boards/Household";
        var one = new object[] { new { key = "a", x = 0, y = 0, w = 2, h = 1 } };

        using var first = await admins.PutAsJsonAsync(url, new { items = one });
        var loaded = (await first.ReadJsonAsync()).GetProperty("updatedAt").GetString();
        using var stale = await admins.PutAsJsonAsync(url, new { items = one, expectedUpdatedAt = "2020-01-01T00:00:00Z" });
        using var missing = await admins.PutAsJsonAsync(url, new { items = one });
        using var forced = await admins.PutAsJsonAsync(url, new { items = one, force = true });
        using var fresh = await admins.PutAsJsonAsync(url, new { items = one, expectedUpdatedAt = (await forced.ReadJsonAsync()).GetProperty("updatedAt").GetString() });

        Assert.NotNull(loaded);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("homemix.board_conflict", await stale.CodeAsync());
        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
        Assert.Equal(HttpStatusCode.OK, forced.StatusCode);
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
    }

    [Fact]
    public async Task The_second_of_two_admins_who_both_save_an_unarranged_board_is_refused()
    {
        var (first, second) = (Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(first);
        api.Households.Set(household, second, HouseholdRole.Admin);
        using var one = api.SignedIn(first);
        using var two = api.SignedIn(second);
        var url = $"/homemix/api/households/{household}/boards/Household";

        var unarranged = await (await two.GetAsync(url)).ReadJsonAsync();
        using var firstSave = await one.PutAsJsonAsync(url, new { items = new object[] { new { key = "a", x = 0, y = 0, w = 2, h = 1 } } });
        using var secondSave = await two.PutAsJsonAsync(url, new { items = new object[] { new { key = "b", x = 0, y = 0, w = 1, h = 1 } } });
        var kept = await (await two.GetAsync(url)).ReadJsonAsync();

        Assert.Equal(JsonValueKind.Null, unarranged.GetProperty("updatedAt").ValueKind);
        Assert.Equal(HttpStatusCode.OK, firstSave.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondSave.StatusCode);
        Assert.Equal("homemix.board_conflict", await secondSave.CodeAsync());
        Assert.Equal(["a"], kept.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("key").GetString()));
    }

    [Fact]
    public async Task A_stale_date_with_force_overwrites_what_someone_else_saved()
    {
        var (admin, other) = (Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(admin);
        api.Households.Set(household, other, HouseholdRole.Admin);
        using var admins = api.SignedIn(admin);
        using var others = api.SignedIn(other);
        var url = $"/homemix/api/households/{household}/boards/Household";
        await admins.PutAsJsonAsync(url, new { items = new object[] { new { key = "a", x = 0, y = 0, w = 2, h = 1 } } });

        using var forced = await others.PutAsJsonAsync(url, new
        {
            items = new object[] { new { key = "b", x = 4, y = 0, w = 3, h = 1 } },
            expectedUpdatedAt = "2020-01-01T00:00:00Z",
            force = true,
        });
        var read = await (await admins.GetAsync(url)).ReadJsonAsync();

        Assert.Equal(HttpStatusCode.OK, forced.StatusCode);
        Assert.Equal(["b"], read.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("key").GetString()));
    }

    [Fact]
    public async Task A_row_changed_under_an_open_editor_fails_the_concurrency_token_in_the_database()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using (var admins = api.SignedIn(admin))
        {
            await admins.PutAsJsonAsync($"/homemix/api/households/{household}/boards/Household", new
            {
                items = new object[] { new { key = "a", x = 0, y = 0, w = 2, h = 1 } },
            });
        }

        await using var editor = api.Services.CreateAsyncScope();
        await using var other = api.Services.CreateAsyncScope();
        var stale = editor.ServiceProvider.GetRequiredService<HomeMixDbContext>();
        var winner = other.ServiceProvider.GetRequiredService<HomeMixDbContext>();
        var items = new[] { new BoardItem("b", 0, 0, 1, 1, "Center") };
        var staleBoard = await stale.Set<WidgetBoard>().SingleAsync(b => b.HouseholdId == household && b.Place == "Household");
        var winnerBoard = await winner.Set<WidgetBoard>().SingleAsync(b => b.HouseholdId == household && b.Place == "Household");
        Assert.True(winnerBoard.Arrange(items, admin, DateTimeOffset.UtcNow.AddSeconds(5)).IsSuccess);
        await winner.SaveChangesAsync();

        Assert.True(staleBoard.Arrange(items, admin, DateTimeOffset.UtcNow.AddSeconds(10)).IsSuccess);
        // The DB race: SaveChanges is where EF sees the lost row; the kit turns it into ConcurrencyConflictException (HTTP 409 concurrency.conflict).
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => stale.SaveChangesAsync());
    }
}
