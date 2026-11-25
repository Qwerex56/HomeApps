using System.Text.Json;
using Devices.Application.Common;
using Devices.Application.Frames;
using Devices.Application.Frames.ProcessFrames;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Devices;
using HomeApps.Contracts.Devices;
using HomeApps.Contracts.Notifications;

namespace Devices.Application.Tests;

public sealed class ProcessFramesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeDevices _devices = new();
    private readonly FakeTelemetry _telemetry = new();
    private readonly FakeStream _stream = new();
    private readonly FakeBus _bus = new();
    private readonly FakeDirectory _directory = new();
    private readonly FakeTime _time = new(Now);
    private readonly Device _device;
    private readonly RecentControlSources _sources;
    private readonly ProcessFramesHandler _handler;

    public ProcessFramesTests()
    {
        _sources = new RecentControlSources(_time);
        _device = PairedDevice.Create(Now);
        _devices.Add(_device);
        _directory.Admins.Add(Guid.NewGuid());
        _handler = new ProcessFramesHandler(_devices, _telemetry, _stream, _bus, new AdminNotices(_directory, _bus, Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminNotices>.Instance), _sources, _time);
    }

    private static JsonElement Json(string json) => JsonElement.Parse(json);

    private async Task<IReadOnlyList<FrameResult>> SendAsync(params DeviceFrame[] frames) =>
        (await _handler.Handle(new ProcessFramesCommand(_device.Id, frames), CancellationToken.None)).Value;

    [Fact]
    public async Task State_is_stored_with_its_time_and_streamed()
    {
        var results = await SendAsync(new DeviceFrame("state", Values: Json("""{ "heating": 2, "temperature": 21.5 }""")));

        Assert.True(results[0].Ok);
        var state = _telemetry.States[_device.Id];
        Assert.Contains("\"heating\":2", state.Values, StringComparison.Ordinal);
        Assert.Equal(Now, state.ValuesAt);
        Assert.Equal("state", Assert.Single(_stream.Messages).Type);
    }

    [Fact]
    public async Task State_that_differs_publishes_DeviceStateChanged_with_only_the_changed_values()
    {
        await SendAsync(new DeviceFrame("state", Values: Json("""{ "heating": 2, "temperature": 21.5 }""")));
        _bus.Published.Clear();

        await SendAsync(new DeviceFrame("state", Values: Json("""{ "heating": 2, "temperature": 22 }""")));

        var changed = Assert.IsType<DeviceStateChanged>(Assert.Single(_bus.Published));
        Assert.Equal(_device.Id, changed.DeviceId);
        Assert.Equal(_device.Slug, changed.Slug);
        Assert.Equal("""{"temperature":22}""", changed.Changed);
        Assert.Equal(Now, changed.At);
    }

    [Fact]
    public async Task State_that_repeats_the_stored_values_publishes_nothing()
    {
        await SendAsync(new DeviceFrame("state", Values: Json("""{ "heating": 2 }""")));
        _bus.Published.Clear();

        await SendAsync(new DeviceFrame("state", Values: Json("""{ "heating": 2 }""")));

        Assert.Empty(_bus.Published);
    }

    [Fact]
    public async Task A_report_is_applied_frame_by_frame_and_a_bad_frame_does_not_stop_the_rest()
    {
        var results = await SendAsync(
            new DeviceFrame("state", Values: Json("""{ "heating": 9 }""")),
            new DeviceFrame("state", Values: Json("""{ "temperature": 20 }""")),
            new DeviceFrame("nonsense"),
            new DeviceFrame("status", Battery: 80));

        Assert.Equal([false, true, false, true], results.Select(r => r.Ok));
        Assert.Equal("devices.invalid_frame", results[0].Code);
        Assert.Equal("devices.unknown_frame", results[2].Code);
        Assert.DoesNotContain("heating", _telemetry.States[_device.Id].Values, StringComparison.Ordinal);
        Assert.Equal(80, _telemetry.States[_device.Id].Battery);
    }

    [Fact]
    public async Task A_declared_event_is_recorded_published_and_streamed_to_members_only()
    {
        await SendAsync(new DeviceFrame("event", Name: "button", At: "2026-11-18T11:59:00Z", Data: Json("""{ "count": 2 }""")));

        var stored = Assert.Single(_telemetry.Events);
        Assert.Equal(Now.AddMinutes(-1), stored.OccurredAt);
        Assert.False(stored.IsLocation);
        var published = Assert.IsType<DeviceEventOccurred>(Assert.Single(_bus.Published));
        Assert.Equal(("button", "{ \"count\": 2 }", _device.HouseholdId), (published.Name, published.Payload, published.HouseholdId));
        Assert.True(Assert.Single(_stream.Messages).MembersOnly);
    }

    [Fact]
    public async Task A_location_event_is_marked_and_a_future_time_becomes_now()
    {
        await SendAsync(new DeviceFrame("event", Name: "tag", At: "2030-01-01T00:00:00Z"));

        var stored = Assert.Single(_telemetry.Events);
        Assert.True(stored.IsLocation);
        Assert.Equal(Now, stored.OccurredAt);
        Assert.True(Assert.IsType<DeviceEventOccurred>(Assert.Single(_bus.Published)).IsLocation);
    }

    [Theory]
    [InlineData("doorbell")]
    [InlineData(null)]
    public async Task An_event_the_descriptor_does_not_declare_is_refused(string? name)
    {
        var results = await SendAsync(new DeviceFrame("event", Name: name));

        Assert.False(results[0].Ok);
        Assert.Empty(_telemetry.Events);
        Assert.Empty(_bus.Published);
    }

    [Fact]
    public async Task A_low_battery_notifies_the_admins_once_per_drop()
    {
        await SendAsync(new DeviceFrame("status", Battery: 50));
        await SendAsync(new DeviceFrame("status", Battery: 19));
        await SendAsync(new DeviceFrame("status", Battery: 18));

        var notice = Assert.IsType<NotificationRequested>(Assert.Single(_bus.Published));
        Assert.Equal(_directory.Admins, notice.UserIds);
        Assert.Equal("battery", notice.Category);

        await SendAsync(new DeviceFrame("status", Battery: 90));
        await SendAsync(new DeviceFrame("status", Battery: 10));
        Assert.Equal(2, _bus.Published.Count);
    }

    [Fact]
    public async Task A_status_reaches_the_stream_and_is_remembered()
    {
        await SendAsync(new DeviceFrame("status", Battery: 78, Voltage: 3.9, Rssi: -61, Uptime: 86400, Firmware: "1.2.0"));

        var state = _telemetry.States[_device.Id];
        Assert.Equal((78, 3.9, -61, 86400, "1.2.0"), (state.Battery, state.Voltage, state.Rssi, state.Uptime, state.Firmware));
        Assert.Equal(Now, state.LastStatusAt);
    }

    [Fact]
    public async Task A_device_without_a_descriptor_sends_no_frames()
    {
        var bare = PairedDevice.Create(Now, descriptor: null);
        _devices.Add(bare);

        var result = await _handler.Handle(new ProcessFramesCommand(bare.Id, [new DeviceFrame("status")]), CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Not_a_device_at_all_is_refused()
    {
        var result = await _handler.Handle(new ProcessFramesCommand(Guid.NewGuid(), [new DeviceFrame("status")]), CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void A_report_has_between_one_and_fifty_frames()
    {
        var validator = new ProcessFramesValidator();

        Assert.False(validator.Validate(new ProcessFramesCommand(Guid.NewGuid(), [])).IsValid);
        Assert.True(validator.Validate(new ProcessFramesCommand(Guid.NewGuid(), [.. Enumerable.Repeat(new DeviceFrame("status"), 50)])).IsValid);
        Assert.False(validator.Validate(new ProcessFramesCommand(Guid.NewGuid(), [.. Enumerable.Repeat(new DeviceFrame("status"), 51)])).IsValid);
    }

    [Fact]
    public async Task The_first_status_after_silence_notifies_the_admins_once()
    {
        await SendAsync(new DeviceFrame("status", Battery: 80));
        _telemetry.States[_device.Id].GoSilent(Now, TimeSpan.FromMinutes(1), Now.AddHours(1));
        _bus.Published.Clear();

        await SendAsync(new DeviceFrame("status", Battery: 80));
        await SendAsync(new DeviceFrame("status", Battery: 80));

        var notice = Assert.IsType<NotificationRequested>(Assert.Single(_bus.Published));
        Assert.Equal("silent", notice.Category);
    }

    [Fact]
    public async Task A_null_frame_is_refused_by_the_validator()
    {
        var validator = new ProcessFramesValidator();

        Assert.False((await validator.ValidateAsync(new ProcessFramesCommand(_device.Id, [null!]))).IsValid);
    }

    [Fact]
    public async Task State_changed_right_after_an_add_on_set_carries_its_source_and_the_device_info()
    {
        await SendAsync(new DeviceFrame("state", Values: Json("""{ "heating": 0, "temperature": 21.5 }""")));
        _bus.Published.Clear();
        _sources.Remember(_device.Id, "heating", "homemix:abc");

        await SendAsync(new DeviceFrame("state", Values: Json("""{ "heating": 2, "temperature": 21.5 }""")));

        var changed = Assert.IsType<DeviceStateChanged>(Assert.Single(_bus.Published));
        Assert.Equal("homemix:abc", changed.ChangedBy);
        Assert.Equal(_device.Name, changed.Device!.Name);
        Assert.Equal(_device.Capabilities, changed.Device.Capabilities);
    }

    [Fact]
    public async Task The_source_is_forgotten_after_five_seconds_and_for_other_controls()
    {
        await SendAsync(new DeviceFrame("state", Values: Json("""{ "heating": 0, "temperature": 21.5 }""")));
        _sources.Remember(_device.Id, "heating", "homemix:abc");
        _time.Now += TimeSpan.FromSeconds(6);
        _bus.Published.Clear();

        await SendAsync(new DeviceFrame("state", Values: Json("""{ "heating": 1, "temperature": 21.5 }""")));
        Assert.Null(Assert.IsType<DeviceStateChanged>(Assert.Single(_bus.Published)).ChangedBy);

        _sources.Remember(_device.Id, "heating", "homemix:abc");
        _bus.Published.Clear();
        await SendAsync(new DeviceFrame("state", Values: Json("""{ "heating": 1, "temperature": 19 }""")));
        Assert.Null(Assert.IsType<DeviceStateChanged>(Assert.Single(_bus.Published)).ChangedBy);
    }
}
