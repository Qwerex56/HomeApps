using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using HomeMix.Application.Abstractions;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "The store": official add-ons that came with HomeMix, then the index's repositories.</summary>
public sealed class StoreTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    [Fact]
    public async Task The_store_lists_official_addons_first_then_the_index()
    {
        api.Store.Listings.Clear();
        api.Store.Listings.Add(new StoreListing(
            "https://github.com/someone/shopping", "shopping", "Lista zakupów", "v1.2.0", new string('a', 64), "# Shopping", null, null));
        using var client = api.SignedIn(Guid.NewGuid());

        var store = await (await client.GetAsync("/homemix/api/store")).ReadJsonAsync();

        Assert.Equal(2, store.GetArrayLength());
        Assert.True(store[0].GetProperty("official").GetBoolean());
        Assert.Equal("weather", store[0].GetProperty("addonId").GetString());
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(HomeMixApiFactory.OfficialWeather)), store[0].GetProperty("sha256").GetString());
        Assert.False(store[1].GetProperty("official").GetBoolean());
        Assert.Equal("https://github.com/someone/shopping", store[1].GetProperty("repository").GetString());
        Assert.Equal("# Shopping", store[1].GetProperty("readme").GetString());
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
        var sha = new string('b', 64);
        api.Store.Listings.Clear();
        api.Store.Listings.Add(new StoreListing("https://github.com/someone/bad", "bad", null, "v1", sha, null, null, null));
        using var owner = api.WithRole(Guid.NewGuid(), "Owner");
        (await owner.PutAsJsonAsync($"/homemix/api/admin/blocks/{sha}", new { reason = "spam" })).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync("/homemix/api/admin/blocks/weather", new { reason = (string?)null })).EnsureSuccessStatusCode();

        var store = await (await owner.GetAsync("/homemix/api/store")).ReadJsonAsync();
        (await owner.DeleteAsync("/homemix/api/admin/blocks/weather")).EnsureSuccessStatusCode();

        Assert.Equal(0, store.GetArrayLength());
    }
}
