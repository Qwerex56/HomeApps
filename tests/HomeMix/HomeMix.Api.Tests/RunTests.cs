using System.Net;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "The sandbox": an add-on's files come with headers that keep it away from HomeApps.</summary>
public sealed class RunTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    [Fact]
    public async Task The_entry_is_served_in_a_sandbox_without_a_token()
    {
        var admin = Guid.NewGuid();
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(api.Household(admin), AddonZip.Pomodoro(version: "3.0.0", network: """["https://api.open-meteo.com"]"""),
            Requests.OpenMeteo);
        using var anonymous = api.CreateClient();

        using var entry = await anonymous.GetAsync($"/homemix/run/{id}/");
        using var script = await anonymous.GetAsync($"/homemix/run/{id}/app.js");

        Assert.Equal(HttpStatusCode.OK, entry.StatusCode);
        Assert.Equal(AddonZip.Index, await entry.Content.ReadAsStringAsync());
        Assert.Equal("text/html", entry.Content.Headers.ContentType!.MediaType);
        var csp = string.Join(' ', entry.Headers.GetValues("Content-Security-Policy"));
        Assert.StartsWith("sandbox allow-scripts allow-forms allow-popups;", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("allow-same-origin", csp, StringComparison.Ordinal);
        Assert.Contains("connect-src https://api.open-meteo.com", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'self'", csp, StringComparison.Ordinal);
        Assert.Equal("*", Assert.Single(entry.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("text/javascript", script.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Without_network_nothing_may_be_fetched()
    {
        var admin = Guid.NewGuid();
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(api.Household(admin), AddonZip.Pomodoro(version: "3.0.1"));

        using var entry = await api.CreateClient().GetAsync($"/homemix/run/{id}/index.html");

        Assert.Contains("connect-src 'none'", string.Join(' ', entry.Headers.GetValues("Content-Security-Policy")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_manifest_and_other_paths_are_not_served()
    {
        var admin = Guid.NewGuid();
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(api.Household(admin), AddonZip.Pomodoro(version: "3.0.2"));
        using var anonymous = api.CreateClient();

        using var manifest = await anonymous.GetAsync($"/homemix/run/{id}/homemix.json");
        using var missing = await anonymous.GetAsync($"/homemix/run/{id}/nope.js");
        using var unknown = await anonymous.GetAsync($"/homemix/run/{Guid.NewGuid()}/index.html");

        Assert.Equal(HttpStatusCode.NotFound, manifest.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task Without_a_trailing_slash_it_redirects()
    {
        var id = Guid.NewGuid();
        using var client = api.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/homemix/run/{id}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/homemix/run/{id}/", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task A_removed_addon_stops_being_served()
    {
        var admin = Guid.NewGuid();
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(api.Household(admin), AddonZip.Pomodoro(version: "3.0.3"));

        (await admins.DeleteAsync($"/homemix/api/installations/{id}")).EnsureSuccessStatusCode();
        using var entry = await api.CreateClient().GetAsync($"/homemix/run/{id}/");

        Assert.Equal(HttpStatusCode.NotFound, entry.StatusCode);
    }
}
