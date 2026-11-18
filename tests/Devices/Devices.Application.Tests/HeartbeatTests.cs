using Devices.Application.Common;
using Devices.Application.Heartbeat.CheckHeartbeats;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Devices;
using HomeApps.Contracts.Devices;
using HomeApps.Contracts.Notifications;

namespace Devices.Application.Tests;

public sealed class HeartbeatTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeDevices _devices = new();
    private readonly FakeTelemetry _telemetry = new();
    private readonly FakeStream _stream = new();
    private readonly FakeBus _bus = new();
    private readonly FakeDirectory _directory = new();
    private readonly FakeTime _time = new(Start);
    private readonly Device _device;

    public HeartbeatTests()
    {
        _device = PairedDevice.Create(Start);
        _devices.Add(_device);
        _directory.Admins.Add(Guid.NewGuid());
    }

    private async Task<int> ScanAsync(DateTimeOffset? notBefore = null) =>
        (await new CheckHeartbeatsHandler(
            _devices, _telemetry, _stream, _bus, new AdminNotices(_directory, _bus, Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminNotices>.Instance), _time)
            .Handle(new CheckHeartbeatsCommand(notBefore ?? Start), CancellationToken.None)).Value;

    [Fact]
    public async Task A_device_missing_for_two_and_a_half_periods_goes_silent_once_and_admins_are_told()
    {
        _time.Now = Start.AddSeconds(150);
        Assert.Equal(0, await ScanAsync());

        _time.Now = Start.AddSeconds(151);
        Assert.Equal(1, await ScanAsync());
        _time.Now = Start.AddMinutes(30);
        Assert.Equal(0, await ScanAsync());

        var silent = Assert.IsType<DeviceWentSilent>(_bus.Published[0]);
        Assert.Equal((_device.HouseholdId, _device.Id), (silent.HouseholdId, silent.DeviceId));
        Assert.Equal(_directory.Admins, Assert.IsType<NotificationRequested>(_bus.Published[1]).UserIds);
        Assert.Equal(2, _bus.Published.Count);
        Assert.True(_telemetry.States[_device.Id].Silent);
    }

    [Fact]
    public async Task A_status_report_keeps_the_device_from_going_silent_and_ends_silence()
    {
        var state = DeviceState.For(_device.Id);
        _telemetry.Add(state);
        state.ApplyStatus(new StatusReport(null, null, null, null, null, null), Start.AddSeconds(100));

        _time.Now = Start.AddSeconds(240);
        Assert.Equal(0, await ScanAsync());
        _time.Now = Start.AddSeconds(251);
        Assert.Equal(1, await ScanAsync());

        state.ApplyStatus(new StatusReport(null, null, null, null, null, null), _time.Now);
        Assert.False(state.Silent);
        _time.Now = _time.Now.AddMinutes(5);
        Assert.Equal(1, await ScanAsync());
    }

    [Fact]
    public async Task Time_before_the_scanning_started_does_not_count()
    {
        _time.Now = Start.AddHours(5);

        Assert.Equal(0, await ScanAsync(notBefore: Start.AddHours(5)));
        _time.Now = Start.AddHours(5).AddSeconds(151);
        Assert.Equal(1, await ScanAsync(notBefore: Start.AddHours(5)));
    }

    [Fact]
    public async Task The_period_comes_from_the_descriptor_and_a_sleepy_device_uses_its_wake_interval()
    {
        var sleepy = PairedDevice.Create(Start, PairedDevice.Chair.Replace("\"heartbeat\": 60", "\"heartbeat\": 900, \"sleepy\": true", StringComparison.Ordinal));
        _devices.All.Clear();
        _devices.Add(sleepy);

        _time.Now = Start.AddSeconds(2249);
        Assert.Equal(0, await ScanAsync());
        _time.Now = Start.AddSeconds(2251);
        Assert.Equal(1, await ScanAsync());
    }

    [Fact]
    public async Task Computers_and_devices_without_a_descriptor_owe_no_heartbeat()
    {
        _devices.Add(PairedDevice.Create(Start, descriptor: null));
        _devices.All.Remove(_device);

        _time.Now = Start.AddDays(1);

        Assert.Equal(0, await ScanAsync());
    }

    [Fact]
    public async Task When_households_is_down_the_contract_still_goes_out_without_a_notice()
    {
        _directory.Down = true;
        _time.Now = Start.AddMinutes(10);

        Assert.Equal(1, await ScanAsync());

        Assert.IsType<DeviceWentSilent>(Assert.Single(_bus.Published));
    }
}
