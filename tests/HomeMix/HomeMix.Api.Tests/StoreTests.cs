using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using HomeMix.Application.Store.RefreshStore;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "The store": built-in add-ons, official entries from addons.json and submitted ones (B3).</summary>
public sealed class StoreTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    private static byte[] Addon(string id, string version) => AddonZip.Create(AddonZip.Manifest(id: id, version: version));

    [Fact]
    public async Task A_submitted_repository_is_listed_under_its_own_key_and_submitting_again_reads_it_again()
    {
        var (ewa, adam) = (Guid.NewGuid(), Guid.NewGuid());
        const string repository = "https://github.com/ewa/chores";
        api.GitHub.Release(repository, "v1.0.0", Addon("chores", "1.0.0"), "# Obowiązki");
        api.GitHub.Release("https://github.com/adam/chores", "v2.0.0", Addon("chores", "2.0.0"));
        using var ewas = api.SignedIn(ewa);
        using var adams = api.SignedIn(adam);

        var first = await (await ewas.PostAsJsonAsync("/homemix/api/store", new { repository = repository + ".git" })).ReadJsonAsync();
        var other = await (await adams.PostAsJsonAsync("/homemix/api/store", new { repository = "https://github.com/adam/chores" })).ReadJsonAsync();
        api.GitHub.Release(repository, "v1.1.0", Addon("chores", "1.1.0"));
        var again = await (await ewas.PostAsJsonAsync("/homemix/api/store", new { repository })).ReadJsonAsync();
        var store = await (await adams.GetAsync("/homemix/api/store")).ReadJsonAsync();
        var page = await (await adams.GetAsync($"/homemix/api/store/{first.GetProperty("listingKey").GetString()}")).ReadJsonAsync();

        var key = first.GetProperty("listingKey").GetString()!;
        Assert.Matches("^chores-[0-9a-f]{8}$", key);
        Assert.NotEqual(key, other.GetProperty("listingKey").GetString());
        Assert.Equal(key, again.GetProperty("listingKey").GetString());
        Assert.Equal("1.1.0", again.GetProperty("version").GetString());
        Assert.True(first.GetProperty("mine").GetBoolean());
        Assert.False(page.GetProperty("mine").GetBoolean());
        Assert.Equal("# Obowiązki", first.GetProperty("readme").GetString());
        Assert.Equal(repository, first.GetProperty("repository").GetString());
        Assert.Equal(2, store.EnumerateArray().Count(e => e.GetProperty("addonId").GetString() == "chores"));
    }

    [Fact]
    public async Task Submissions_are_limited_and_a_repository_without_a_release_is_refused()
    {
        var someone = Guid.NewGuid();
        using var client = api.SignedIn(someone);

        using var none = await client.PostAsJsonAsync("/homemix/api/store", new { repository = "https://github.com/someone/empty" });
        var codes = new List<string?>();
        for (var i = 0; i < 6; i++)
        {
            var repository = $"https://github.com/someone/addon{i}";
            api.GitHub.Release(repository, "v1", Addon($"addon{i}", "1.0.0"));
            using var response = await client.PostAsJsonAsync("/homemix/api/store", new { repository });
            codes.Add(response.IsSuccessStatusCode ? null : await response.CodeAsync());
        }

        Assert.Equal("homemix.store_unusable", await none.CodeAsync());
        Assert.Equal([null, null, null, null, null, "homemix.store_limit"], codes);
    }

    [Fact]
    public async Task The_author_refreshes_and_removes_others_may_not_and_the_owner_hides()
    {
        var (author, stranger) = (Guid.NewGuid(), Guid.NewGuid());
        const string repository = "https://github.com/author/notes";
        api.GitHub.Release(repository, "v1", Addon("notes", "1.0.0"));
        using var authors = api.SignedIn(author);
        using var strangers = api.SignedIn(stranger);
        using var owner = api.WithRole(Guid.NewGuid(), "Owner");
        var key = (await (await authors.PostAsJsonAsync("/homemix/api/store", new { repository })).ReadJsonAsync())
            .GetProperty("listingKey").GetString();

        api.GitHub.Release(repository, "v2", Addon("notes", "2.0.0"));
        var refreshed = await (await authors.PostAsync($"/homemix/api/store/{key}/refresh", null)).ReadJsonAsync();
        api.GitHub.Release(repository, "v3", Addon("other", "3.0.0"));
        var changed = await (await authors.PostAsync($"/homemix/api/store/{key}/refresh", null)).ReadJsonAsync();
        using var notMine = await strangers.DeleteAsync($"/homemix/api/store/{key}");
        using var hidden = await owner.PutAsJsonAsync($"/homemix/api/admin/store/{key}/hidden", new { hidden = true });
        using var hiddenForOthers = await strangers.GetAsync($"/homemix/api/store/{key}");
        using var hiddenForAuthor = await authors.GetAsync($"/homemix/api/store/{key}");
        using var removed = await authors.DeleteAsync($"/homemix/api/store/{key}");
        using var gone = await authors.GetAsync($"/homemix/api/store/{key}");

        Assert.Equal("2.0.0", refreshed.GetProperty("version").GetString());
        Assert.Equal("2.0.0", changed.GetProperty("version").GetString());
        Assert.Equal("id_changed", changed.GetProperty("problem").GetString());
        Assert.Equal("homemix.not_author", await notMine.CodeAsync());
        Assert.Equal("Hidden", (await hidden.ReadJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NotFound, hiddenForOthers.StatusCode);
        Assert.Equal(HttpStatusCode.OK, hiddenForAuthor.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task Addons_json_becomes_official_entries_and_the_image_wins()
    {
        api.Store.Repositories.Clear();
        api.Store.Repositories.AddRange(["https://github.com/team/timer", "https://github.com/team/weather"]);
        api.GitHub.Release("https://github.com/team/timer", "v1", Addon("timer", "1.0.0"));
        api.GitHub.Release("https://github.com/team/weather", "v9", Addon("weather", "9.0.0"));

        await RefreshStoreAsync();
        await RefreshStoreAsync();
        using var client = api.SignedIn(Guid.NewGuid());
        var store = await (await client.GetAsync("/homemix/api/store")).ReadJsonAsync();
        api.Store.Repositories.Clear();

        var official = store.EnumerateArray().Where(e => e.GetProperty("official").GetBoolean()).ToList();
        Assert.Equal("weather", official[0].GetProperty("addonId").GetString());
        Assert.True(official[0].GetProperty("builtin").GetBoolean());
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(HomeMixApiFactory.OfficialWeather)), official[0].GetProperty("sha256").GetString());
        var timer = Assert.Single(official, e => e.GetProperty("addonId").GetString() == "timer");
        Assert.Equal("timer-official", timer.GetProperty("listingKey").GetString());
        Assert.False(timer.GetProperty("builtin").GetBoolean());
        Assert.DoesNotContain(official, e => e.GetProperty("version").GetString() == "9.0.0");
        Assert.Equal(1, api.GitHub.Downloads["https://github.com/team/timer"]);
    }

    [Fact]
    public async Task An_official_addon_is_added_by_id_and_installed_like_any_other()
    {
        var admin = Guid.NewGuid();
        using var admins = api.SignedIn(admin);

        using var added = await admins.PostAsync("/homemix/api/packages/official/weather", null);
        var package = await added.ReadJsonAsync();
        using var installed = await admins.InstallInAsync(api.Household(admin), package.GetProperty("sha256").GetString()!, Requests.Weather);
        using var unknown = await admins.PostAsync("/homemix/api/packages/official/pomodoro", null);

        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        Assert.True(package.GetProperty("official").GetBoolean());
        Assert.Equal(HttpStatusCode.Created, installed.StatusCode);
        Assert.True((await installed.ReadJsonAsync()).GetProperty("official").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task A_blocked_addon_leaves_the_store()
    {
        const string repository = "https://github.com/someone/bad";
        api.GitHub.Release(repository, "v1", Addon("badaddon", "1.0.0"));
        using var client = api.SignedIn(Guid.NewGuid());
        (await client.PostAsJsonAsync("/homemix/api/store", new { repository })).EnsureSuccessStatusCode();
        using var owner = api.WithRole(Guid.NewGuid(), "Owner");
        (await owner.PutAsJsonAsync("/homemix/api/admin/blocks/badaddon", new { reason = "spam" })).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync("/homemix/api/admin/blocks/weather", new { reason = (string?)null })).EnsureSuccessStatusCode();

        var store = await (await owner.GetAsync("/homemix/api/store")).ReadJsonAsync();
        (await owner.DeleteAsync("/homemix/api/admin/blocks/weather")).EnsureSuccessStatusCode();
        (await owner.DeleteAsync("/homemix/api/admin/blocks/badaddon")).EnsureSuccessStatusCode();

        Assert.DoesNotContain(store.EnumerateArray(), e => e.GetProperty("addonId").GetString() is "badaddon" or "weather");
    }

    [Fact]
    public async Task Another_addon_with_the_same_id_cannot_stand_in_the_same_place()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        api.GitHub.Release("https://github.com/one/lists", "v1", Addon("lists", "1.0.0"));
        api.GitHub.Release("https://github.com/two/lists", "v1", Addon("lists", "1.0.1"));

        var one = await (await admins.PostAsJsonAsync("/homemix/api/packages/github", new { repository = "https://github.com/one/lists" })).ReadJsonAsync();
        var two = await (await admins.PostAsJsonAsync("/homemix/api/packages/github", new { repository = "https://github.com/two/lists" })).ReadJsonAsync();
        (await admins.InstallInAsync(household, one.GetProperty("sha256").GetString()!)).EnsureSuccessStatusCode();
        using var second = await admins.InstallInAsync(household, two.GetProperty("sha256").GetString()!);

        Assert.Equal("homemix.id_in_use", await second.CodeAsync());
    }

    private async Task RefreshStoreAsync()
    {
        await using var scope = api.Services.CreateAsyncScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RefreshStoreCommand())).IsSuccess);
    }
}
