using Devices.Application.Telemetry.GetDeviceEvents;
using Devices.Application.Telemetry.GetDeviceState;
using Devices.Application.Telemetry.GetStreamAccess;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.Tests;

public sealed class ReadingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeDevices _devices = new();
    private readonly FakeTelemetry _telemetry = new();
    private readonly Device _device = PairedDevice.Create(Now);

    public ReadingTests()
    {
        _devices.Add(_device);
        _telemetry.Add(DeviceEvent.Record(_device.Id, _device.HouseholdId!.Value, "button", null, Now.AddMinutes(-2), false));
        _telemetry.Add(DeviceEvent.Record(_device.Id, _device.HouseholdId.Value, "button", null, Now.AddMinutes(-1), false));
    }

    [Fact]
    public async Task A_guest_reads_state_but_not_events()
    {
        var roles = new FakeRoles(HouseholdRole.Guest);

        var state = await new GetDeviceStateHandler(_devices, _telemetry, roles, new FakeLinks()).Handle(new GetDeviceStateQuery(_device.Id), CancellationToken.None);
        var events = await new GetDeviceEventsHandler(_devices, _telemetry, roles).Handle(new GetDeviceEventsQuery(_device.Id), CancellationToken.None);

        Assert.True(state.IsSuccess);
        Assert.True(events.IsFailed);
    }

    [Fact]
    public async Task A_member_reads_events_newest_first_in_pages()
    {
        var handler = new GetDeviceEventsHandler(_devices, _telemetry, new FakeRoles(HouseholdRole.Member));

        var first = (await handler.Handle(new GetDeviceEventsQuery(_device.Id, 0, 1), CancellationToken.None)).Value;
        var second = (await handler.Handle(new GetDeviceEventsQuery(_device.Id, 1, 1), CancellationToken.None)).Value;

        Assert.Equal(Now.AddMinutes(-1), first[0].OccurredAt);
        Assert.Equal(Now.AddMinutes(-2), second[0].OccurredAt);
    }

    [Theory]
    [InlineData(HouseholdRole.Guest, false)]
    [InlineData(HouseholdRole.Member, true)]
    public async Task The_stream_has_events_only_for_members(HouseholdRole role, bool events)
    {
        var access = await new GetStreamAccessHandler(new FakeRoles(role)).Handle(new GetStreamAccessQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(events, access.Value);
    }

    [Fact]
    public async Task A_stranger_gets_nothing()
    {
        var access = await new GetStreamAccessHandler(new FakeRoles(null)).Handle(new GetStreamAccessQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(access.IsFailed);
    }
}
