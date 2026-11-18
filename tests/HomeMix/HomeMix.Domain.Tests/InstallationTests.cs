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

    [Fact]
    public void Widget_choices_follow_the_manifest_and_survive_an_update()
    {
        const string twoSizes = """
            "widgets": [{ "id": "timer", "title": { "pl": "Minutnik" }, "size": "Small", "sizes": ["Small", "Large"], "places": ["Household", "Wall"] }]
            """;
        const string oneSize = """
            "widgets": [{ "id": "timer", "title": { "pl": "Minutnik" }, "size": "Small", "places": ["Household", "Wall"] }]
            """;
        var package = Package(extra: twoSizes);
        var installation = Installation.Install(package, InstallTarget.Household(Guid.NewGuid()), Household, Adam, Now).Value;

        var account = installation.ChooseWidget(package.Manifest, WidgetPlaces.Account, "timer", null, 0);
        installation.ChooseWidget(package.Manifest, WidgetPlaces.Wall, "timer", "Large", 1);
        installation.Update(Package(version: "1.1.0", extra: oneSize), consent: null, Adam, Now.AddDays(1));

        Assert.Equal("homemix.widget_place", Assert.Single(account.Errors).Metadata[AppError.CodeKey]);
        Assert.Equal(new WidgetChoice(WidgetPlaces.Wall, "timer", "Small", 1), Assert.Single(installation.Widgets));
    }

    private const string DeviceWidgets = """
        "widgets": [ { "id": "stacja", "title": { "pl": "Stacja" }, "devices": [ { "capability": "weather" } ] }, { "id": "zegar", "title": { "pl": "Zegar" } } ]
        """;

    [Fact]
    public void A_device_is_picked_for_a_widget_that_matches_devices_and_changed_by_picking_again()
    {
        var package = Package(capabilities: """["devices"]""", extra: DeviceWidgets);
        var installation = Installation.Install(package, InstallTarget.Household(Guid.NewGuid()), new Consent(["devices"], [], false), Adam, Now).Value;
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        Assert.True(installation.PickDevice(package.Manifest, "stacja", first).IsSuccess);
        Assert.True(installation.PickDevice(package.Manifest, "stacja", second).IsSuccess);

        Assert.Equal(new DevicePick("stacja", second), Assert.Single(installation.DevicePicks));
        Assert.True(installation.PickDevice(package.Manifest, "stacja", null).IsSuccess);
        Assert.Empty(installation.DevicePicks);
    }

    [Fact]
    public void A_widget_that_asks_for_no_devices_takes_no_pick()
    {
        var package = Package(capabilities: """["devices"]""", extra: DeviceWidgets);
        var installation = Installation.Install(package, InstallTarget.Household(Guid.NewGuid()), new Consent(["devices"], [], false), Adam, Now).Value;

        var result = installation.PickDevice(package.Manifest, "zegar", Guid.NewGuid());

        Assert.Equal("homemix.widget_without_devices", Assert.Single(result.Errors).Metadata[AppError.CodeKey]);
    }

    [Fact]
    public void Installing_a_device_control_add_on_needs_consent_to_it()
    {
        var package = Package(capabilities: """["devices", "devices.control"]""");

        var result = Installation.Install(package, InstallTarget.Household(Guid.NewGuid()), new Consent(["devices"], [], false), Adam, Now);

        Assert.Equal("homemix.consent_required", Assert.Single(result.Errors).Metadata[AppError.CodeKey]);
    }
}
