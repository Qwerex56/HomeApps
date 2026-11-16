using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "Numbers and domki": installs counted per store entry, hearts, the store's tabs and "Konto" (B4).</summary>
public sealed class StoreStatsTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    private static byte[] Addon(string id, string version, string? extra = null) =>
        AddonZip.Create(AddonZip.Manifest(id: id, version: version, extra: extra));

    private static async Task<string> SubmitAsync(HttpClient client, string repository) =>
        (await (await client.PostAsJsonAsync("/homemix/api/store", new { repository })).ReadJsonAsync()).GetProperty("listingKey").GetString()!;

    private static async Task<string> FromGitHubAsync(HttpClient client, string repository) =>
        (await (await client.PostAsJsonAsync("/homemix/api/packages/github", new { repository })).ReadJsonAsync()).GetProperty("sha256").GetString()!;

    private static async Task<Guid> InstallAsync(HttpClient client, Guid household, string sha256)
    {
        using var response = await client.InstallInAsync(household, sha256);
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync()).GetProperty("id").GetGuid();
    }

    private static JsonElement Entry(JsonElement list, string key) =>
        list.EnumerateArray().Single(e => e.GetProperty("listingKey").GetString() == key);

    [Fact]
    public async Task Installs_updates_and_removals_count_for_the_entry_and_its_author_sees_them_in_konto()
    {
        var (author, admin) = (Guid.NewGuid(), Guid.NewGuid());
        const string repository = "https://github.com/ewa/counted";
        api.GitHub.Release(repository, "v1.0.0", Addon("counted", "1.0.0"));
        using var authors = api.SignedIn(author);
        using var admins = api.SignedIn(admin);
        var key = await SubmitAsync(authors, repository);

        var v1 = await FromGitHubAsync(admins, repository);
        var first = await InstallAsync(admins, api.Household(admin), v1);
        var second = await InstallAsync(admins, api.Household(admin), v1);
        api.GitHub.Release(repository, "v1.1.0", Addon("counted", "1.1.0"));
        var v11 = await FromGitHubAsync(admins, repository);
        (await admins.PutAsJsonAsync($"/homemix/api/installations/{first}", new { sha256 = v11 })).EnsureSuccessStatusCode();
        (await admins.DeleteAsync($"/homemix/api/installations/{second}")).EnsureSuccessStatusCode();
        await admins.InstalledInAsync(api.Household(admin), Addon("counted", "9.9.9"));

        var mine = await (await authors.GetAsync("/homemix/api/store/mine")).ReadJsonAsync();
        var others = await (await admins.GetAsync("/homemix/api/store/mine")).ReadJsonAsync();
        var page = await (await admins.GetAsync($"/homemix/api/store/{key}")).ReadJsonAsync();

        var listing = Assert.Single(mine.EnumerateArray());
        var counts = listing.GetProperty("counts");
        Assert.Equal(key, listing.GetProperty("entry").GetProperty("listingKey").GetString());
        Assert.Equal(1, counts.GetProperty("active").GetInt32());
        Assert.Equal(1, counts.GetProperty("households").GetInt32());
        Assert.Equal(0, counts.GetProperty("accounts").GetInt32());
        Assert.Equal(2, counts.GetProperty("installs").GetInt32());
        Assert.Equal(2, counts.GetProperty("installs30").GetInt32());
        Assert.Equal(1, counts.GetProperty("updates").GetInt32());
        Assert.Equal(1, counts.GetProperty("removals").GetInt32());
        var version = Assert.Single(listing.GetProperty("versions").EnumerateArray());
        Assert.Equal("1.1.0", version.GetProperty("version").GetString());
        var days = listing.GetProperty("days").EnumerateArray().ToList();
        Assert.Equal(30, days.Count);
        Assert.Equal(2, days[^1].GetProperty("installs").GetInt32());
        Assert.Empty(others.EnumerateArray());
        Assert.Equal(1, page.GetProperty("installs").GetInt32());
        Assert.DoesNotContain("installedBy", listing.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_domek_is_given_once_per_person_and_can_be_taken_back()
    {
        var (ewa, adam) = (Guid.NewGuid(), Guid.NewGuid());
        const string repository = "https://github.com/ewa/liked";
        api.GitHub.Release(repository, "v1", Addon("liked", "1.0.0"));
        using var ewas = api.SignedIn(ewa);
        using var adams = api.SignedIn(adam);
        var key = await SubmitAsync(ewas, repository);

        (await ewas.PutAsync($"/homemix/api/store/{key}/heart", null)).EnsureSuccessStatusCode();
        (await ewas.PutAsync($"/homemix/api/store/{key}/heart", null)).EnsureSuccessStatusCode();
        (await adams.PutAsync($"/homemix/api/store/{key}/heart", null)).EnsureSuccessStatusCode();
        (await adams.PutAsync("/homemix/api/store/weather-official/heart", null)).EnsureSuccessStatusCode();
        var both = await (await ewas.GetAsync($"/homemix/api/store/{key}")).ReadJsonAsync();
        (await adams.DeleteAsync($"/homemix/api/store/{key}/heart")).EnsureSuccessStatusCode();
        (await adams.DeleteAsync($"/homemix/api/store/{key}/heart")).EnsureSuccessStatusCode();
        var store = await (await adams.GetAsync("/homemix/api/store")).ReadJsonAsync();
        using var unknown = await adams.PutAsync("/homemix/api/store/nothing-12345678/heart", null);
        using var assistant = api.Assistant(adam);
        using var byAssistant = await assistant.PutAsync($"/homemix/api/store/{key}/heart", null);

        Assert.Equal(2, both.GetProperty("hearts").GetInt32());
        Assert.True(both.GetProperty("hearted").GetBoolean());
        Assert.Equal(1, Entry(store, key).GetProperty("hearts").GetInt32());
        Assert.False(Entry(store, key).GetProperty("hearted").GetBoolean());
        Assert.True(Entry(store, "weather-official").GetProperty("hearted").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byAssistant.StatusCode);
    }

    [Fact]
    public async Task Tabs_sort_and_filters_narrow_the_store()
    {
        var (author, admin) = (Guid.NewGuid(), Guid.NewGuid());
        using var authors = api.SignedIn(author);
        using var admins = api.SignedIn(admin);
        api.GitHub.Release("https://github.com/tabs/quiet", "v1", Addon("tabquiet", "1.0.0", extra: "\"jarvis\": true"));
        api.GitHub.Release("https://github.com/tabs/busy", "v1", Addon(
            "tabbusy", "1.0.0", extra: "\"description\": { \"pl\": \"Dużo instalacji\", \"en\": \"Many installs\" }"));
        var quiet = await SubmitAsync(authors, "https://github.com/tabs/quiet");
        var busy = await SubmitAsync(authors, "https://github.com/tabs/busy");
        var sha = await FromGitHubAsync(admins, "https://github.com/tabs/busy");
        await InstallAsync(admins, api.Household(admin), sha);
        (await authors.PutAsync($"/homemix/api/store/{quiet}/heart", null)).EnsureSuccessStatusCode();

        async Task<List<string>> KeysAsync(string query) =>
            [.. (await (await admins.GetAsync($"/homemix/api/store?{query}")).ReadJsonAsync()).EnumerateArray()
                .Select(e => e.GetProperty("listingKey").GetString()!)
                .Where(k => k == quiet || k == busy || k == "weather-official")];

        Assert.Equal([busy, quiet], (await KeysAsync("tab=popular")).Where(k => k != "weather-official"));
        Assert.Equal([quiet, busy], (await KeysAsync("tab=hearts")).Where(k => k != "weather-official"));
        Assert.Equal([busy, quiet], await KeysAsync("tab=new"));
        Assert.Equal(["weather-official"], await KeysAsync("tab=featured"));
        Assert.Equal([busy], await KeysAsync("q=many%20INSTALLS"));
        Assert.Equal([quiet], await KeysAsync("jarvis=true"));
        Assert.DoesNotContain("weather-official", await KeysAsync("offline=true"));
        Assert.Empty(await KeysAsync("page=2"));
        using var bad = await admins.GetAsync("/homemix/api/store?tab=best");
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task The_owner_lists_every_entry_including_hidden_ones()
    {
        const string repository = "https://github.com/hide/me";
        api.GitHub.Release(repository, "v1", Addon("hideme", "1.0.0"));
        using var someone = api.SignedIn(Guid.NewGuid());
        using var owner = api.WithRole(Guid.NewGuid(), "Owner");
        var key = await SubmitAsync(someone, repository);
        (await owner.PutAsJsonAsync($"/homemix/api/admin/store/{key}/hidden", new { hidden = true })).EnsureSuccessStatusCode();

        var all = await (await owner.GetAsync("/homemix/api/admin/store")).ReadJsonAsync();
        using var forbidden = await someone.GetAsync("/homemix/api/admin/store");
        var store = await (await someone.GetAsync("/homemix/api/store")).ReadJsonAsync();

        Assert.Equal("Hidden", Entry(all, key).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.DoesNotContain(store.EnumerateArray(), e => e.GetProperty("listingKey").GetString() == key);
    }
}
