using System.Text.Json;
using Devices.Application.Common;
using Devices.Application.Controls.SetControlForAddon;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Devices;
using HomeApps.BuildingBlocks.Domain.Errors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Devices.Application.Tests;

public sealed class SetControlForAddonTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeDevices _devices = new();
    private readonly FakeLinks _links = new();
    private readonly FakeDirectory _directory = new();
    private readonly FakeTelemetry _telemetry = new();
    private readonly FakeTime _time = new(Now);
    private readonly RecentControlSources _sources;
    private readonly Guid _household = Guid.NewGuid();
    private readonly Guid _installer = Guid.NewGuid();
    private readonly Guid _installation = Guid.NewGuid();
    private readonly Device _device;

    public SetControlForAddonTests()
    {
        _sources = new RecentControlSources(_time);
        _device = PairedDevice.Create(Now, householdId: _household);
        _devices.Add(_device);
        _directory.Admins.Add(_installer);
    }

    private Task<FluentResults.Result> SetAsync(Guid? installer = null, Guid? household = null, string control = "heating", string value = "2") =>
        new SetControlForAddonHandler(_devices, _links, _directory, _sources, _telemetry, _time, NullLogger<SetControlForAddonHandler>.Instance)
            .Handle(new SetControlForAddonCommand(
                _device.Id, household ?? _household, control, JsonElement.Parse(value), installer ?? _installer, _installation, "Termostat"),
                CancellationToken.None);

    private static string Code(FluentResults.Result result) => ((AppError)result.Errors[0]).Code;

    [Fact]
    public async Task An_admin_installer_lets_the_add_on_set_the_control_and_the_history_says_so()
    {
        var result = await SetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal((_device.Id, "heating", "2"), Assert.Single(_links.Sets));
        var entry = Assert.Single(_telemetry.Events);
        Assert.Equal("set_by_addon", entry.Name);
        Assert.Contains("ustawione przez dodatek Termostat", entry.Data, StringComparison.Ordinal);
        Assert.Equal($"homemix:{_installation}", _sources.Find(_device.Id, ["heating"]));
    }

    [Fact]
    public async Task An_installer_who_is_no_longer_an_admin_is_refused()
    {
        var result = await SetAsync(installer: Guid.NewGuid());

        Assert.Equal("devices.forbidden", Code(result));
        Assert.Empty(_links.Sets);
        Assert.Empty(_telemetry.Events);
    }

    [Fact]
    public async Task A_device_of_another_household_looks_like_it_does_not_exist()
    {
        var result = await SetAsync(household: Guid.NewGuid());

        Assert.Equal("devices.not_found", Code(result));
        Assert.Empty(_links.Sets);
    }

    [Fact]
    public async Task The_value_is_checked_against_the_control_and_Households_being_down_refuses()
    {
        Assert.True((await SetAsync(value: "9")).IsFailed);
        Assert.Empty(_links.Sets);

        _directory.Down = true;
        Assert.Equal("devices.members_unavailable", Code(await SetAsync()));
        Assert.Empty(_links.Sets);
    }

    [Fact]
    public async Task An_offline_device_leaves_no_history()
    {
        _links.Online = false;

        Assert.True((await SetAsync()).IsFailed);
        Assert.Empty(_telemetry.Events);
    }
}
