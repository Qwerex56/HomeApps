using HomeApps.BuildingBlocks.Domain.Errors;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;

namespace HomeMix.Domain.Tests;

public sealed class InstallationTests
{
    private static readonly Guid Adam = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    private static AddonPackage Package(string version = "1.0.0", string capabilities = """["storage.household"]""",
        string network = "[]", string? targets = null, string id = "pomodoro", string? extra = null) =>
        AddonPackage.Create(
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(version + capabilities + network + id))),
            Manifests.Valid(id: id, version: version, capabilities: capabilities, network: network, targets: targets, extra: extra),
            [new PackageFile("homemix.json", 100), new PackageFile("index.html", 10)],
            1024, null, null, Adam, Now).Value;

    private static readonly Consent Household = new(["storage.household"], [], Jarvis: false);

    [Fact]
    public void Installing_grants_what_the_package_asks_for()
    {
        var installation = Installation.Install(Package(), InstallTarget.Household(Guid.NewGuid()), Household, Adam, Now).Value;

        Assert.Equal(["storage.household"], installation.Capabilities);
        Assert.True(installation.IsActive);
        Assert.Equal("1.0.0", installation.Version);
    }

    [Fact]
    public void Installing_needs_consent_for_everything_asked()
    {
        var package = Package(network: """["https://api.open-meteo.com"]""");

        var result = Installation.Install(package, InstallTarget.Household(Guid.NewGuid()), Household, Adam, Now);

        var error = Assert.Single(result.Errors);
        Assert.Equal("homemix.consent_required", error.Metadata[AppError.CodeKey]);
        Assert.Contains("https://api.open-meteo.com", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_household_only_addon_is_not_installed_on_an_account()
    {
        var result = Installation.Install(Package(targets: """["Household"]"""), InstallTarget.User(Adam), Household, Adam, Now);

        Assert.Equal("homemix.target_not_supported", Assert.Single(result.Errors).Metadata[AppError.CodeKey]);
    }

    [Fact]
    public void Jarvis_needs_both_the_addon_and_the_installer()
    {
        var package = Package(extra: "\"jarvis\": true");

        var refused = Installation.Install(package, InstallTarget.User(Adam), Household, Adam, Now).Value;
        var agreed = Installation.Install(package, InstallTarget.User(Adam), Household with { Jarvis = true }, Adam, Now).Value;

        Assert.False(refused.JarvisAllowed);
        Assert.True(agreed.JarvisAllowed);
    }

    [Fact]
    public void An_update_asking_for_no_more_applies_at_once()
    {
        var installation = Installation.Install(Package(), InstallTarget.User(Adam), Household, Adam, Now).Value;

        var result = installation.Update(Package(version: "1.1.0", capabilities: "[]"), consent: null, Adam, Now.AddDays(1));

        Assert.True(result.IsSuccess);
        Assert.Equal("1.1.0", installation.Version);
        Assert.Empty(installation.Capabilities);
    }

    [Fact]
    public void An_update_asking_for_more_needs_a_new_consent()
    {
        var installation = Installation.Install(Package(), InstallTarget.User(Adam), Household, Adam, Now).Value;
        var more = Package(version: "2.0.0", capabilities: """["storage.household", "notify"]""");

        var refused = installation.Update(more, consent: null, Adam, Now);
        var accepted = installation.Update(more, new Consent(["storage.household", "notify"], [], false), Adam, Now);

        Assert.Equal("homemix.consent_required", Assert.Single(refused.Errors).Metadata[AppError.CodeKey]);
        Assert.True(accepted.IsSuccess);
        Assert.Equal("2.0.0", installation.Version);
    }

    [Fact]
    public void An_update_must_be_the_same_addon()
    {
        var installation = Installation.Install(Package(), InstallTarget.User(Adam), Household, Adam, Now).Value;

        var result = installation.Update(Package(id: "weather"), Household, Adam, Now);

        Assert.Equal("homemix.other_addon", Assert.Single(result.Errors).Metadata[AppError.CodeKey]);
    }

    [Fact]
    public void Removing_keeps_the_row_and_happens_once()
    {
        var installation = Installation.Install(Package(), InstallTarget.User(Adam), Household, Adam, Now).Value;

        Assert.True(installation.Remove(Adam, Now).IsSuccess);
        Assert.False(installation.IsActive);
        Assert.True(installation.Remove(Adam, Now).IsFailed);
    }
}
