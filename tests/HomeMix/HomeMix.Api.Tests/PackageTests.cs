using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "Where packages come from": a zip or a GitHub release becomes a package to consent to.</summary>
public sealed class PackageTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    [Fact]
    public async Task A_zip_answers_what_the_addon_asks_for()
    {
        using var client = api.SignedIn(Guid.NewGuid());
        var zip = AddonZip.Pomodoro(network: """["https://api.open-meteo.com"]""");

        using var response = await client.UploadAsync(zip);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var package = await response.ReadJsonAsync();
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(zip)), package.GetProperty("sha256").GetString());
        Assert.Equal("pomodoro", package.GetProperty("id").GetString());
        Assert.Equal("Pomodoro", package.GetProperty("name").GetProperty("pl").GetString());
        Assert.Equal(["Household", "User"], package.GetProperty("targets").EnumerateArray().Select(t => t.GetString()));
        Assert.Equal("https://api.open-meteo.com", package.GetProperty("network")[0].GetString());
    }

    [Fact]
    public async Task The_same_zip_twice_is_one_package()
    {
        var zip = AddonZip.Pomodoro(version: "1.0.1");
        using var adam = api.SignedIn(Guid.NewGuid());
        using var ewa = api.SignedIn(Guid.NewGuid());

        Assert.Equal(await adam.PackageAsync(zip), await ewa.PackageAsync(zip));
    }

    [Theory]
    [InlineData("jarvis")]
    [InlineData("households")]
    [InlineData("todo")]
    public async Task Ids_of_HomeApps_and_its_plugins_are_reserved(string id)
    {
        using var client = api.SignedIn(Guid.NewGuid());

        using var response = await client.UploadAsync(AddonZip.Create(AddonZip.Manifest(id: id)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("homemix.id_reserved", await response.CodeAsync());
    }

    [Fact]
    public async Task A_blocked_addon_is_refused()
    {
        using var client = api.SignedIn(Guid.NewGuid());

        using var response = await client.UploadAsync(AddonZip.Create(AddonZip.Manifest(id: "evil")));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("homemix.blocked", await response.CodeAsync());
    }

    [Fact]
    public async Task Paths_outside_the_package_are_refused()
    {
        using var client = api.SignedIn(Guid.NewGuid());
        var zip = AddonZip.Create(AddonZip.Manifest(), ("index.html", AddonZip.Index), ("../escape.js", "alert(1)"));

        using var response = await client.UploadAsync(zip);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("homemix.package_invalid", await response.CodeAsync());
    }

    [Fact]
    public async Task Not_a_zip_is_refused()
    {
        using var client = api.SignedIn(Guid.NewGuid());

        using var response = await client.UploadAsync("hello"u8.ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("homemix.package_invalid", await response.CodeAsync());
    }

    [Fact]
    public async Task A_GitHub_release_becomes_a_package_with_its_source()
    {
        const string repository = "https://github.com/someone/weather";
        api.GitHub.Release(repository, "v1.2.0", AddonZip.Create(AddonZip.Manifest(id: "weather", version: "1.2.0")));
        using var client = api.SignedIn(Guid.NewGuid());

        using var response = await client.PostAsJsonAsync("/homemix/api/packages/github", new { repository });
        using var wrong = await client.PostAsJsonAsync("/homemix/api/packages/github", new { repository = "https://gitlab.com/someone/weather" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var package = await response.ReadJsonAsync();
        Assert.Equal("weather", package.GetProperty("id").GetString());
        Assert.Equal(repository, package.GetProperty("repository").GetString());
        Assert.Equal("v1.2.0", package.GetProperty("tag").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
    }

    [Fact]
    public async Task Adding_needs_a_signed_in_person()
    {
        using var anonymous = api.CreateClient();

        using var response = await anonymous.UploadAsync(AddonZip.Pomodoro());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
