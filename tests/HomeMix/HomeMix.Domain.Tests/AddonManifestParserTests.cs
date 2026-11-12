using HomeApps.BuildingBlocks.Domain.Errors;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;

namespace HomeMix.Domain.Tests;

public sealed class AddonManifestParserTests
{
    [Fact]
    public void A_minimal_manifest_gets_both_targets_and_one_page()
    {
        var manifest = AddonManifestParser.Parse(Manifests.Valid()).Value;

        Assert.Equal("pomodoro", manifest.Id);
        Assert.Equal([TargetKind.Household, TargetKind.User], manifest.Targets);
        var page = Assert.Single(manifest.Pages);
        Assert.Equal(string.Empty, page.Path);
        Assert.Equal("Pomodoro", page.Title.Pl);
        Assert.False(manifest.Jarvis);
    }

    [Fact]
    public void Every_problem_is_reported_at_once()
    {
        var result = AddonManifestParser.Parse(Manifests.Valid(
            id: "Pomodoro", version: "one", capabilities: """["storage.everything"]""", network: """["http://example.com"]"""));

        Assert.True(result.IsFailed);
        Assert.All(result.Errors, e => Assert.Equal("homemix.manifest_invalid", e.Metadata[AppError.CodeKey]));
        Assert.Equal(4, result.Errors.Count);
    }

    [Theory]
    [InlineData("https://api.open-meteo.com", true)]
    [InlineData("https://api.open-meteo.com/", true)]
    [InlineData("https://api.open-meteo.com:8443", true)]
    [InlineData("https://api.open-meteo.com/v1", false)]
    [InlineData("https://*.open-meteo.com", false)]
    [InlineData("http://api.open-meteo.com", false)]
    [InlineData("https://user@api.open-meteo.com", false)]
    public void Network_takes_https_origins_only(string origin, bool accepted)
    {
        var result = AddonManifestParser.Parse(Manifests.Valid(network: $"[\"{origin}\"]"));

        Assert.Equal(accepted, result.IsSuccess);
    }

    [Fact]
    public void Origins_are_kept_without_a_trailing_slash()
    {
        var manifest = AddonManifestParser.Parse(Manifests.Valid(network: """["https://a.example/", "https://A.example"]""")).Value;

        Assert.Equal(["https://a.example"], manifest.Network);
    }

    [Theory]
    [InlineData("\"entry\": \"../index.html\"")]
    [InlineData("\"entry\": \"/index.html\"")]
    [InlineData("\"entry\": \"index.js\"")]
    public void The_entry_is_a_relative_html_file(string entry)
    {
        var json = Manifests.Valid().Replace("\"entry\": \"index.html\"", entry, StringComparison.Ordinal);

        Assert.True(AddonManifestParser.Parse(json).IsFailed);
    }

    [Fact]
    public void Not_json_is_an_invalid_manifest()
    {
        var result = AddonManifestParser.Parse("{ nope");

        Assert.Equal("homemix.manifest_invalid", Assert.Single(result.Errors).Metadata[AppError.CodeKey]);
    }

    [Fact]
    public void Widgets_and_permissions_are_read()
    {
        var manifest = AddonManifestParser.Parse(Manifests.Valid(extra: """
            "widgets": [{ "id": "timer", "title": { "pl": "Minutnik" }, "size": "Small", "places": ["Household", "Wall"] }],
            "permissions": [{ "id": "reset", "minRole": "Admin" }],
            "jarvis": true
            """)).Value;

        var widget = Assert.Single(manifest.Widgets);
        Assert.Equal("Minutnik", widget.Title.En);
        Assert.Equal(["Household", "Wall"], widget.Places);
        Assert.Equal(new AddonPermission("reset", "Admin"), Assert.Single(manifest.Permissions));
        Assert.True(manifest.Jarvis);
    }

    [Theory]
    [InlineData("2.3.0", "2.3.0", true)]
    [InlineData("2.3.0", "2.2.9", true)]
    [InlineData("2.3.0", "2.10.0", false)]
    [InlineData("2.3.0-rc.1", "2.3.0", true)]
    public void Versions_compare_by_number(string version, string minimum, bool atLeast) =>
        Assert.Equal(atLeast, AddonVersion.AtLeast(version, minimum));
}
