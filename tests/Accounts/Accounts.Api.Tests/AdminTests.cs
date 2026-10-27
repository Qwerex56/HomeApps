using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Accounts.Api.Tests;

public sealed class AdminTests(AccountsApiFactory api) : IClassFixture<AccountsApiFactory>
{
    [Fact]
    public async Task List_filters_searches_and_pages()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await api.RegisterAsync(displayName: $"Lista {marker} A");
        await api.RegisterAsync(displayName: $"Lista {marker} B");
        await api.RegisterAsync(displayName: $"Lista {marker} C", role: HomeApps.Contracts.Auth.ServerRole.Admin);
        using var admin = api.SignedIn(Guid.NewGuid(), "Admin");

        var page = await (await admin.GetAsync($"/admin/users?q={marker}&pageSize=2&page=2&sort=DisplayName&desc=true")).ReadJsonAsync();
        Assert.Equal(3, page.GetProperty("total").GetInt32());
        Assert.Equal([$"Lista {marker} A"], page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("displayName").GetString()));

        var admins = await (await admin.GetAsync($"/admin/users?q={marker}&role=Admin")).ReadJsonAsync();
        Assert.Equal("Admin", Assert.Single(admins.GetProperty("items").EnumerateArray()).GetProperty("role").GetString());

        using var bad = await admin.GetAsync("/admin/users?pageSize=1000");
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Admin_edits_disables_enables_and_reads_history()
    {
        var id = await api.RegisterAsync(displayName: "Zenek");
        var adminId = await api.RegisterAsync(displayName: "Szefowa", role: HomeApps.Contracts.Auth.ServerRole.Admin);
        using var admin = api.SignedIn(adminId, "Admin");

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/admin/users/{id}/profile", AccountsApiFactory.Profile("Zenon"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/admin/users/{id}/disable", new { reason = "Spam" })).StatusCode);

        var user = await (await admin.GetAsync($"/admin/users/{id}")).ReadJsonAsync();
        Assert.Equal("Zenon", user.GetProperty("profile").GetProperty("displayName").GetString());
        Assert.Equal(adminId, user.GetProperty("disabling").GetProperty("byUserId").GetGuid());
        Assert.Equal("Spam", user.GetProperty("disabling").GetProperty("reason").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/admin/users/{id}/enable", null)).StatusCode);

        var history = await (await admin.GetAsync($"/admin/users/{id}/history")).ReadJsonAsync();
        Assert.Equal(["Enabled", "Disabled", "ProfileUpdated", "Registered"],
            history.EnumerateArray().Select(e => e.GetProperty("action").GetString()));
        Assert.Equal("Szefowa", history[1].GetProperty("actorName").GetString());

        var before = Uri.EscapeDataString(history[1].GetProperty("at").GetString()!);
        var older = await (await admin.GetAsync($"/admin/users/{id}/history?before={before}")).ReadJsonAsync();
        Assert.Equal(["ProfileUpdated", "Registered"], older.EnumerateArray().Select(e => e.GetProperty("action").GetString()));
    }

    [Fact]
    public async Task Admin_cannot_touch_another_admin()
    {
        var other = await api.RegisterAsync(role: HomeApps.Contracts.Auth.ServerRole.Admin);
        using var admin = api.SignedIn(Guid.NewGuid(), "Admin");

        using var response = await admin.PostAsJsonAsync($"/admin/users/{other}/disable", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("forbidden.role", await response.CodeAsync());
    }

    [Fact]
    public async Task Owner_erases_a_disabled_account_with_its_email_in_the_body()
    {
        var email = AccountsApiFactory.Unique("zapomnij");
        var id = await api.RegisterAsync(email);
        using var owner = api.SignedIn(Guid.NewGuid(), "Owner");
        await owner.PostAsJsonAsync($"/admin/users/{id}/disable", new { });

        using var wrong = await Delete(owner, id, "ktos.inny@example.com");
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal("erase.confirmation_mismatch", await wrong.CodeAsync());

        using var erased = await Delete(owner, id, email);
        Assert.Equal(HttpStatusCode.NoContent, erased.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/admin/users/{id}")).StatusCode);
    }

    [Fact]
    public async Task Admin_is_not_allowed_to_erase()
    {
        var email = AccountsApiFactory.Unique("nie");
        var id = await api.RegisterAsync(email);
        using var admin = api.SignedIn(Guid.NewGuid(), "Admin");
        await admin.PostAsJsonAsync($"/admin/users/{id}/disable", new { });

        using var response = await Delete(admin, id, email);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static Task<HttpResponseMessage> Delete(HttpClient client, Guid id, string confirmEmail) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/admin/users/{id}")
        {
            Content = JsonContent.Create(new { confirmEmail }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        });
}
