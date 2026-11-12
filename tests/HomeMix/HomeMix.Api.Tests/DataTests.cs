using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.Contracts.Households;
using HomeMix.Application.Data.PurgeRemovedData;
using HomeMix.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "Data": each installation's own space of JSON documents.</summary>
public sealed class DataTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    private static readonly string Everything = AddonZip.Manifest(
        id: "planner", capabilities: """["storage.household", "storage.user"]""",
        extra: """ "permissions": [{ "id": "plan.write", "minRole": "Admin" }, { "id": "notes.read", "minRole": "Guest" }] """);

    private static readonly object Consent = new { capabilities = new[] { "storage.household", "storage.user" } };

    private sealed record Household(Guid Id, Guid Installation, Guid Admin, Guid Member, Guid Guest);

    private async Task<Household> HouseholdAsync(string manifest, object consent)
    {
        var (admin, member, guest) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Member);
        api.Households.Set(household, guest, HouseholdRole.Guest);
        using var admins = api.SignedIn(admin);
        var installation = await admins.InstalledInAsync(household, AddonZip.Create(manifest), consent);
        return new Household(household, installation, admin, member, guest);
    }

    private static string Url(Guid installation, string collection, string? key = null, string? scope = null) =>
        $"/homemix/api/installations/{installation}/data/{collection}{(key is null ? string.Empty : "/" + key)}{(scope is null ? string.Empty : "?scope=" + scope)}";

    [Fact]
    public async Task Members_write_guests_read_strangers_see_nothing()
    {
        var home = await HouseholdAsync(AddonZip.Manifest(id: "timer"), Requests.Household);
        using var member = api.SignedIn(home.Member);
        using var guest = api.SignedIn(home.Guest);
        using var stranger = api.SignedIn(Guid.NewGuid());

        using var put = await member.PutAsJsonAsync(Url(home.Installation, "sessions", "a1"), new { value = new { minutes = 25 } });
        var read = await guest.GetFromJsonAsync<JsonElement>(Url(home.Installation, "sessions"));
        using var guestPut = await guest.PutAsJsonAsync(Url(home.Installation, "sessions", "a2"), new { value = 1 });
        using var foreign = await stranger.GetAsync(Url(home.Installation, "sessions"));

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var document = Assert.Single(read.EnumerateArray());
        Assert.Equal("a1", document.GetProperty("key").GetString());
        Assert.Equal(25, document.GetProperty("value").GetProperty("minutes").GetInt32());
        Assert.Equal(1, document.GetProperty("version").GetInt32());
        Assert.Equal(home.Member, document.GetProperty("updatedBy").GetGuid());
        Assert.Equal(HttpStatusCode.Forbidden, guestPut.StatusCode);
        Assert.Equal("homemix.read_only", await guestPut.CodeAsync());
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
    }

    [Fact]
    public async Task A_permission_named_after_a_collection_sets_who_writes_it()
    {
        var home = await HouseholdAsync(Everything, Consent);
        using var member = api.SignedIn(home.Member);
        using var admin = api.SignedIn(home.Admin);

        using var memberPlan = await member.PutAsJsonAsync(Url(home.Installation, "plan", "week"), new { value = "x" });
        using var adminPlan = await admin.PutAsJsonAsync(Url(home.Installation, "plan", "week"), new { value = "x" });
        using var memberNotes = await member.PutAsJsonAsync(Url(home.Installation, "notes", "n1"), new { value = "x" });
        var memberAccess = await member.GetFromJsonAsync<JsonElement>($"/homemix/api/installations/{home.Installation}/me");
        var adminAccess = await admin.GetFromJsonAsync<JsonElement>($"/homemix/api/installations/{home.Installation}/me");

        Assert.Equal("homemix.read_only", await memberPlan.CodeAsync());
        Assert.Equal(HttpStatusCode.OK, adminPlan.StatusCode);
        Assert.Equal(HttpStatusCode.OK, memberNotes.StatusCode);
        Assert.Equal("Member", memberAccess.GetProperty("role").GetString());
        Assert.Equal(["notes.read"], memberAccess.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal(["plan.write", "notes.read"], adminAccess.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
    }

    [Fact]
    public async Task Private_data_is_seen_only_by_its_person_and_guests_have_their_own()
    {
        var home = await HouseholdAsync(Everything, Consent);
        using var member = api.SignedIn(home.Member);
        using var guest = api.SignedIn(home.Guest);

        (await member.PutAsJsonAsync(Url(home.Installation, "prefs", "me", "user"), new { value = "member's" })).EnsureSuccessStatusCode();
        using var guestPut = await guest.PutAsJsonAsync(Url(home.Installation, "prefs", "me", "user"), new { value = "guest's" });
        var memberSees = await member.GetFromJsonAsync<JsonElement>(Url(home.Installation, "prefs", "me", "user"));
        var guestSees = await guest.GetFromJsonAsync<JsonElement>(Url(home.Installation, "prefs", scope: "user"));
        using var shared = await member.GetAsync(Url(home.Installation, "prefs", "me"));

        Assert.Equal(HttpStatusCode.OK, guestPut.StatusCode);
        Assert.Equal("member's", memberSees.GetProperty("value").GetString());
        Assert.Equal("guest's", Assert.Single(guestSees.EnumerateArray()).GetProperty("value").GetString());
        Assert.Equal(HttpStatusCode.NotFound, shared.StatusCode);
    }

    [Fact]
    public async Task A_scope_needs_its_capability()
    {
        var home = await HouseholdAsync(AddonZip.Manifest(id: "counter"), Requests.Household);
        using var member = api.SignedIn(home.Member);

        using var response = await member.GetAsync(Url(home.Installation, "prefs", scope: "user"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("homemix.capability_missing", await response.CodeAsync());
    }

    [Fact]
    public async Task A_stale_version_is_a_conflict()
    {
        var home = await HouseholdAsync(AddonZip.Manifest(id: "shopping"), Requests.Household);
        using var member = api.SignedIn(home.Member);
        var url = Url(home.Installation, "items", "milk");

        using var first = await member.PutAsJsonAsync(url, new { value = 1 });
        using var second = await member.PutAsJsonAsync(url, new { value = 2, version = 1 });
        using var stale = await member.PutAsJsonAsync(url, new { value = 3, version = 1 });
        using var staleDelete = await member.DeleteAsync(url + "?version=1");
        using var newWithVersion = await member.PutAsJsonAsync(Url(home.Installation, "items", "bread"), new { value = 1, version = 4 });
        using var delete = await member.DeleteAsync(url + "?version=2");
        using var gone = await member.GetAsync(url);

        Assert.Equal(2, (await second.ReadJsonAsync()).GetProperty("version").GetInt32());
        Assert.Equal("homemix.conflict", await stale.CodeAsync());
        Assert.Equal(HttpStatusCode.Conflict, staleDelete.StatusCode);
        Assert.Equal("homemix.conflict", await newWithVersion.CodeAsync());
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task Limits_hold_per_document_collection_and_installation()
    {
        var home = await HouseholdAsync(AddonZip.Manifest(id: "photos"), Requests.Household);
        using var member = api.SignedIn(home.Member);
        var big = new string('x', 3500);

        using var tooLarge = await member.PutAsJsonAsync(Url(home.Installation, "a", "1"), new { value = new string('x', 5000) });
        for (var i = 0; i < 3; i++)
        {
            (await member.PutAsJsonAsync(Url(home.Installation, "small", $"k{i}"), new { value = i })).EnsureSuccessStatusCode();
        }

        using var fourth = await member.PutAsJsonAsync(Url(home.Installation, "small", "k3"), new { value = 3 });
        using var replace = await member.PutAsJsonAsync(Url(home.Installation, "small", "k0"), new { value = 9 });
        HttpStatusCode last = HttpStatusCode.OK;
        for (var i = 0; i < 20 && last == HttpStatusCode.OK; i++)
        {
            using var put = await member.PutAsJsonAsync(Url(home.Installation, $"c{i}", "big"), new { value = big });
            last = put.StatusCode;
            if (last != HttpStatusCode.OK)
            {
                Assert.Equal("homemix.storage_full", await put.CodeAsync());
            }
        }

        Assert.Equal("homemix.document_too_large", await tooLarge.CodeAsync());
        Assert.Equal("homemix.collection_full", await fourth.CodeAsync());
        Assert.Equal(HttpStatusCode.OK, replace.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, last);
    }

    [Fact]
    public async Task Names_must_fit_a_url_segment()
    {
        var home = await HouseholdAsync(AddonZip.Manifest(id: "names"), Requests.Household);
        using var member = api.SignedIn(home.Member);

        using var response = await member.PutAsJsonAsync(Url(home.Installation, "-bad", "k"), new { value = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reinstalling_within_30_days_brings_the_data_back_and_later_it_is_deleted()
    {
        var home = await HouseholdAsync(AddonZip.Manifest(id: "diary"), Requests.Household);
        using var admin = api.SignedIn(home.Admin);
        (await admin.PutAsJsonAsync(Url(home.Installation, "days", "monday"), new { value = "sun" })).EnsureSuccessStatusCode();

        (await admin.DeleteAsync($"/homemix/api/installations/{home.Installation}")).EnsureSuccessStatusCode();
        var again = await admin.InstalledInAsync(home.Id, AddonZip.Create(AddonZip.Manifest(id: "diary", version: "1.0.1")));
        var back = await admin.GetFromJsonAsync<JsonElement>(Url(again, "days", "monday"));

        (await admin.DeleteAsync($"/homemix/api/installations/{again}")).EnsureSuccessStatusCode();
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomeMixDbContext>();
            await db.Installations.Where(i => i.Id == again)
                .ExecuteUpdateAsync(u => u.SetProperty(i => i.RemovedAt, DateTimeOffset.UtcNow.AddDays(-31)));
            var purged = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new PurgeRemovedDataCommand());
            Assert.True(purged.Value >= 1);
            Assert.False(await db.Documents.AnyAsync(d => d.InstallationId == again));
        }

        Assert.Equal("sun", back.GetProperty("value").GetString());
    }
}
