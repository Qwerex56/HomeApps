using System.Text.Json;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Devices;
using HomeMix.Application.Devices.ChooseWidgetDevice;
using HomeMix.Application.Devices.GetInstallationDeviceState;
using HomeMix.Application.Devices.ListInstallationDevices;
using HomeMix.Application.Devices.SetInstallationDevice;
using HomeMix.Application.Installations;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;

namespace HomeMix.Application.Tests;

public sealed class DeviceCapabilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Adam = Guid.NewGuid();
    private static readonly Guid HouseholdId = Guid.NewGuid();

    private static readonly HouseholdDevice Station = new(
        Guid.NewGuid(), "Stacja", "stacja", "smart_home", true, "a/stacja", ["weather_station"], ["weather", "power"], null);

    private static readonly HouseholdDevice Relay = new(
        Guid.NewGuid(), "Przekaznik", "przekaznik", "smart_home", true, "a/relay", ["light"], ["switch"], null);

    private static string Manifest(string capabilities) =>
        $$"""
        {
          "homemixVersion": 1, "id": "stacja", "version": "1.0.0", "requires": { "homeApps": "2.3.0" },
          "name": { "pl": "Stacja" }, "entry": "index.html", "targets": ["Household", "User"],
          "capabilities": {{capabilities}}{{(capabilities.Contains("\"devices\"", StringComparison.Ordinal) ? Widget : string.Empty)}}
        }
        """;

    private const string Widget = """
        , "widgets": [ { "id": "pogoda", "title": { "pl": "Pogoda" }, "devices": [ { "capability": "weather", "pick": "manual" } ] } ]
        """;

    private static (Installation Installation, AddonPackage Package) Install(string capabilities, InstallTarget? target = null)
    {
        var package = AddonPackage.Create(
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(capabilities))), Manifest(capabilities),
            [new PackageFile("homemix.json", 100), new PackageFile("index.html", 10)], 1024, null, null, Adam, Now).Value;
        var consent = new Consent(package.Manifest.Capabilities, [], false);
        return (Installation.Install(package, target ?? InstallTarget.Household(HouseholdId), consent, Adam, Now).Value, package);
    }

    private static DevicesAccess Access(Installation installation, AddonPackage package, HouseholdRole? role, FakeDevices devices, Guid? me = null) =>
        new(new FakeInstallations(installation), new FakePackages(package), new FakeRoles(role), devices, new FakeUser(me ?? Adam), FakeBlocks.Blocklist());

    private static string Code(FluentResults.Result result) => Assert.IsType<AppError>(Assert.Single(result.Errors)).Code;

    [Fact]
    public async Task Setting_a_control_without_devices_control_is_refused_and_nothing_is_sent()
    {
        var (installation, package) = Install("""["devices"]""");
        var devices = new FakeDevices(Station);
        var handler = new SetInstallationDeviceHandler(Access(installation, package, HouseholdRole.Member, devices), devices);

        var result = await handler.Handle(new SetInstallationDeviceCommand(installation.Id, Station.Id, "on", JsonDocument.Parse("true").RootElement), CancellationToken.None);

        Assert.Equal("homemix.capability_missing", Code(result));
        Assert.Empty(devices.Sets);
    }

    [Fact]
    public async Task With_devices_control_a_member_sets_a_control_of_a_household_device()
    {
        var (installation, package) = Install("""["devices", "devices.control"]""");
        var devices = new FakeDevices(Relay);
        var handler = new SetInstallationDeviceHandler(Access(installation, package, HouseholdRole.Member, devices), devices);

        var result = await handler.Handle(new SetInstallationDeviceCommand(installation.Id, Relay.Id, "on", JsonDocument.Parse("true").RootElement), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((Relay.Id, "on"), (devices.Sets[0].Device, devices.Sets[0].Control));
    }

    [Fact]
    public async Task A_device_of_another_household_is_not_found_even_with_devices_control()
    {
        var (installation, package) = Install("""["devices", "devices.control"]""");
        var devices = new FakeDevices(Relay);
        var handler = new SetInstallationDeviceHandler(Access(installation, package, HouseholdRole.Member, devices), devices);

        var result = await handler.Handle(new SetInstallationDeviceCommand(installation.Id, Guid.NewGuid(), "on", JsonDocument.Parse("true").RootElement), CancellationToken.None);

        Assert.Equal("homemix.device_not_found", Code(result));
        Assert.Empty(devices.Sets);
    }

    [Fact]
    public async Task Without_the_devices_capability_nothing_is_listed_or_read()
    {
        var (installation, package) = Install("""["storage.household"]""");
        var devices = new FakeDevices(Station);
        var access = Access(installation, package, HouseholdRole.Member, devices);

        var list = await new ListInstallationDevicesHandler(access, devices).Handle(
            new ListInstallationDevicesQuery(installation.Id, null, null, null, null, false), CancellationToken.None);
        var state = await new GetInstallationDeviceStateHandler(access, devices).Handle(
            new GetInstallationDeviceStateQuery(installation.Id, Station.Id), CancellationToken.None);

        Assert.Equal("homemix.capability_missing", Code(list.ToResult()));
        Assert.Equal("homemix.capability_missing", Code(state.ToResult()));
    }

    [Fact]
    public async Task Someone_outside_the_household_sees_no_installation()
    {
        var (installation, package) = Install("""["devices"]""");
        var devices = new FakeDevices(Station);

        var list = await new ListInstallationDevicesHandler(Access(installation, package, role: null, devices), devices).Handle(
            new ListInstallationDevicesQuery(installation.Id, null, null, null, null, false), CancellationToken.None);

        Assert.Equal("homemix.installation_not_found", Code(list.ToResult()));
    }

    [Fact]
    public async Task An_account_installation_has_no_household_devices()
    {
        var (installation, package) = Install("""["devices"]""", InstallTarget.User(Adam));
        var devices = new FakeDevices(Station);

        var list = await new ListInstallationDevicesHandler(Access(installation, package, HouseholdRole.Member, devices), devices).Handle(
            new ListInstallationDevicesQuery(installation.Id, null, null, null, null, false), CancellationToken.None);

        Assert.Equal("homemix.devices_household_only", Code(list.ToResult()));
    }

    [Fact]
    public async Task A_widget_lists_only_matching_devices_and_asks_for_a_pick()
    {
        var (installation, package) = Install("""["devices"]""");
        var devices = new FakeDevices(Relay, Station);
        var handler = new ListInstallationDevicesHandler(Access(installation, package, HouseholdRole.Guest, devices), devices);

        var view = (await handler.Handle(new ListInstallationDevicesQuery(installation.Id, "pogoda", null, null, null, true), CancellationToken.None)).Value;

        Assert.Equal(Station.Id, Assert.Single(view.Devices).Id);
        Assert.Null(view.Selected);
        Assert.True(view.NeedsPick);
    }

    [Fact]
    public async Task A_state_is_read_for_a_device_of_the_household()
    {
        var (installation, package) = Install("""["devices"]""");
        var devices = new FakeDevices(Station);

        var state = await new GetInstallationDeviceStateHandler(Access(installation, package, HouseholdRole.Guest, devices), devices).Handle(
            new GetInstallationDeviceStateQuery(installation.Id, Station.Id), CancellationToken.None);

        Assert.True(state.Value.GetProperty("values").GetProperty("on").GetBoolean());
    }

    [Fact]
    public async Task An_account_installation_may_neither_set_nor_read_a_state()
    {
        var (installation, package) = Install("""["devices", "devices.control"]""", InstallTarget.User(Adam));
        var devices = new FakeDevices(Station);
        var access = Access(installation, package, HouseholdRole.Member, devices);

        var set = await new SetInstallationDeviceHandler(access, devices).Handle(
            new SetInstallationDeviceCommand(installation.Id, Station.Id, "on", JsonDocument.Parse("true").RootElement), CancellationToken.None);
        var state = await new GetInstallationDeviceStateHandler(access, devices).Handle(
            new GetInstallationDeviceStateQuery(installation.Id, Station.Id), CancellationToken.None);

        Assert.Equal("homemix.devices_household_only", Code(set));
        Assert.Equal("homemix.devices_household_only", Code(state.ToResult()));
        Assert.Empty(devices.Sets);
    }
}
