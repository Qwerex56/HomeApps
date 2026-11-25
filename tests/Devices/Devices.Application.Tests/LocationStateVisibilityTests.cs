using Devices.Application.Common;
using Devices.Application.Frames;
using Devices.Application.Frames.ProcessFrames;
using Devices.Application.HouseholdDevices.GetHouseholdDevices;
using Devices.Application.Telemetry.GetDeviceState;
using Devices.Application.Tests.Fakes;
using Devices.Application.Tools.DescribeDevices;
using Devices.Application.Tools.JarvisDevices;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;
using Microsoft.Extensions.Logging.Abstractions;

namespace Devices.Application.Tests;

/// <summary>H-4c item 9 (R3): below Member nobody reads the values of a household's <c>geo</c> device; its health stays visible.</summary>
public sealed class LocationStateVisibilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid _household = Guid.NewGuid();
    private readonly FakeDevices _devices = new();
    private readonly FakeTelemetry _telemetry = new();
    private readonly FakeLinks _links = new();
    private readonly Device _tag;
    private readonly Device _chair;

    public LocationStateVisibilityTests()
    {
        _tag = LocationTag.Create(Now, _household);
        _chair = PairedDevice.Create(Now, PairedDevice.Chair, _household);
        _devices.Add(_tag);
        _devices.Add(_chair);
        _telemetry.Add(LocationTag.StateOf(_tag, Now));
        var chairState = DeviceState.For(_chair.Id);
        chairState.ApplyValues(System.Text.Json.JsonElement.Parse("""{ "heating": 2 }"""), Now);
        _telemetry.Add(chairState);
    }

    [Fact]
    public void The_tag_has_the_geo_capability_and_the_chair_does_not()
    {
        Assert.True(_tag.IsLocationDevice);
        Assert.False(_chair.IsLocationDevice);
    }

    private async Task<Devices.Application.Common.DeviceStateView> StateAsync(Device device, HouseholdRole role) =>
        (await new GetDeviceStateHandler(_devices, _telemetry, new FakeRoles(role, _household), _links).Handle(new GetDeviceStateQuery(device.Id), CancellationToken.None)).Value;

    [Fact]
    public async Task A_guest_reading_state_of_a_geo_device_gets_empty_values_and_no_time_but_keeps_health()
    {
        var state = await StateAsync(_tag, HouseholdRole.Guest);

        Assert.Equal("{}", state.Values.GetRawText());
        Assert.Null(state.ValuesAt);
        Assert.Equal(41, state.Battery);
        Assert.Equal(-60, state.Rssi);
        Assert.NotNull(state.LastStatusAt);
        Assert.True(state.Online);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Admin)]
    public async Task A_member_and_up_read_the_values_of_a_geo_device(HouseholdRole role)
    {
        var state = await StateAsync(_tag, role);

        Assert.Equal("kitchen", state.Values.GetProperty("location").GetProperty("room").GetString());
        Assert.Equal(Now, state.ValuesAt);
    }

    [Fact]
    public async Task A_guest_still_reads_the_values_of_a_device_without_geo()
    {
        var state = await StateAsync(_chair, HouseholdRole.Guest);

        Assert.Equal(2, state.Values.GetProperty("heating").GetInt32());
        Assert.Equal(Now, state.ValuesAt);
    }

    [Theory]
    [InlineData(HouseholdRole.Guest, true)]
    [InlineData(HouseholdRole.Member, false)]
    public async Task The_list_with_state_hides_geo_values_from_a_guest_only(HouseholdRole role, bool hidden)
    {
        var list = (await new GetHouseholdDevicesHandler(_devices, new FakeRoles(role, _household), _links, _telemetry)
            .Handle(new GetHouseholdDevicesQuery(_household, WithState: true), CancellationToken.None)).Value;

        var tag = list.Single(d => d.Id == _tag.Id).State!;
        var chair = list.Single(d => d.Id == _chair.Id).State!;
        Assert.Equal(hidden, tag.Values.GetRawText() == "{}");
        Assert.Equal(hidden ? null : Now, tag.ValuesAt);
        Assert.Equal(41, tag.Battery);
        Assert.Equal(2, chair.Values.GetProperty("heating").GetInt32());
    }

    [Theory]
    [InlineData(HouseholdRole.Guest, true)]
    [InlineData(HouseholdRole.Member, false)]
    public async Task devices_describe_follows_the_same_rule(HouseholdRole role, bool hidden)
    {
        var described = (await new DescribeDevicesHandler(_devices, _telemetry, new FakeRoles(role, _household), _links, new FakeUser(Guid.NewGuid()))
            .Handle(new DescribeDevicesQuery(_household), CancellationToken.None)).Value;

        var tag = described.Single(d => d.Id == _tag.Id);
        Assert.Equal(hidden, tag.State.GetRawText() == "{}");
        Assert.Equal(41, tag.Battery);
        Assert.Equal(2, described.Single(d => d.Id == _chair.Id).State.GetProperty("heating").GetInt32());
    }

    private async Task<IReadOnlyList<Devices.Application.Common.DeviceDescriptionView>> ForJarvisAsync(FakeDirectory directory, Guid user) =>
        (await new GetJarvisDevicesHandler(_devices, _telemetry, _links, directory, NullLogger<GetJarvisDevicesHandler>.Instance)
            .Handle(new GetJarvisDevicesQuery(_household, user), CancellationToken.None)).Value;

    [Fact]
    public async Task JARVIS_prompt_path_hides_geo_values_from_a_guest_and_a_satellite_but_not_from_a_member()
    {
        var guest = Guid.NewGuid();
        var member = Guid.NewGuid();
        var directory = new FakeDirectory();
        directory.Guests.Add(guest);
        directory.Members.Add(member);

        var forGuest = await ForJarvisAsync(directory, guest);
        var forMember = await ForJarvisAsync(directory, member);
        var forSatellite = await ForJarvisAsync(directory, _chair.Id);

        Assert.Equal("{}", forGuest.Single(d => d.Id == _tag.Id).State.GetRawText());
        Assert.Equal(41, forGuest.Single(d => d.Id == _tag.Id).Battery);
        Assert.Equal("kitchen", forMember.Single(d => d.Id == _tag.Id).State.GetProperty("location").GetProperty("room").GetString());
        Assert.Equal("{}", forSatellite.Single(d => d.Id == _tag.Id).State.GetRawText());
        Assert.Equal(2, forGuest.Single(d => d.Id == _chair.Id).State.GetProperty("heating").GetInt32());
    }

    [Fact]
    public async Task JARVIS_prompt_path_gives_a_stranger_nothing()
    {
        Assert.Empty(await ForJarvisAsync(new FakeDirectory(), Guid.NewGuid()));
    }

    private ProcessFramesHandler Frames(FakeStream stream)
    {
        var bus = new FakeBus();
        var time = new FakeTime(Now);
        return new ProcessFramesHandler(_devices, _telemetry, stream, bus,
            new AdminNotices(new FakeDirectory(), bus, NullLogger<AdminNotices>.Instance), new RecentControlSources(time), time);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task The_state_message_of_the_stream_is_for_members_only_exactly_when_the_device_is_geo(bool geo)
    {
        var device = geo ? _tag : _chair;
        var stream = new FakeStream();
        var values = geo ? """{ "location": { "room": "hall" } }""" : """{ "heating": 1 }""";

        await Frames(stream).Handle(
            new ProcessFramesCommand(device.Id, [new DeviceFrame("state", Values: System.Text.Json.JsonElement.Parse(values))]), CancellationToken.None);

        var message = Assert.Single(stream.Messages);
        Assert.Equal("state", message.Type);
        Assert.Equal(geo, message.MembersOnly);
    }
}
