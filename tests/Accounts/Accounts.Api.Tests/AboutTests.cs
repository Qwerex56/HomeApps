using System.Net;
using System.Text.Json;

namespace Accounts.Api.Tests;

/// <summary>Req 2.3: GET /health/version (anonymous, every service) and "About the server" for Admin and Owner.</summary>
public sealed class AboutTests(AccountsApiFactory api) : IClassFixture<AccountsApiFactory>
{
    [Fact]
    public async Task Health_version_names_the_service_its_version_and_its_applied_migrations()
    {
        using var anonymous = api.CreateClient();

        using var response = await anonymous.GetAsync("/health/version");
        var body = await response.ReadJsonAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // The name is the entry assembly's: "Accounts" in the service, "testhost" under the test host.
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("service").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("version").GetString()));
        Assert.Equal(JsonValueKind.Array, body.GetProperty("migrations").ValueKind);
        Assert.NotEmpty(body.GetProperty("migrations").EnumerateArray());
    }

    [Fact]
    public async Task About_the_server_lists_every_service_and_is_open_to_admins_only()
    {
        using var admin = api.SignedIn(Guid.NewGuid(), "Admin");
        using var user = api.SignedIn(Guid.NewGuid(), "User");

        using var forbidden = await user.GetAsync("/admin/users/v1/server/about");
        using var response = await admin.GetAsync("/admin/users/v1/server/about");
        var body = await response.ReadJsonAsync();

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("currentVersion").GetString()));
        Assert.Contains(body.GetProperty("services").EnumerateArray(), s => s.GetProperty("name").GetString() == "Accounts");
        Assert.Equal(7, body.GetProperty("services").GetArrayLength());

        // The sandbox may or may not reach GitHub: either the answer is known or it is null, never a false claim.
        var update = body.GetProperty("isUpdateAvailable");
        Assert.True(update.ValueKind is JsonValueKind.Null or JsonValueKind.True or JsonValueKind.False);
        if (update.ValueKind is JsonValueKind.Null)
        {
            Assert.Equal(JsonValueKind.Null, body.GetProperty("latestVersion").ValueKind);
        }
    }
}
