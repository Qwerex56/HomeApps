using System.Text.Json;
using Devices.Application.Common;
using Devices.Application.Controls.SetControl;
using Devices.Application.Frames;
using Devices.Application.Frames.ProcessFrames;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.Tests;

/// <summary>jarvis-desktop-rework 2.8: a control of a sleepy device waits as a pending setting until the device reports it.</summary>
public sealed class SleepyDownlinkTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly string SleepyChair = PairedDevice.Chair.Replace("\"heartbeat\": 60", "\"sleepy\": true", StringComparison.Ordinal);

    private readonly FakeDevices _devices = new();
    private readonly FakeTelemetry _telemetry = new();
    private readonly FakeStream _stream = new();
    private readonly FakeLinks _links = new();
    private readonly FakeTime _time = new(Now);
    private readonly Device _device;

    public SleepyDownlinkTests()
    {
        _device = PairedDevice.Create(Now, SleepyChair);
        _devices.Add(_device);
    }

    private static JsonElement Json(string json) => JsonElement.Parse(json);

    private Task<FluentResults.Result> SetAsync(string value) =>
        new SetControlHandler(_devices, new FakeRoles(HouseholdRole.Member), _links, _telemetry, _stream)
            .Handle(new SetControlCommand(_device.Id, "heating", Json(value)), CancellationToken.None);

    private async Task<ReportResponseView> ReportAsync(params DeviceFrame[] frames)
    {
        var directory = new FakeDirectory();
        var bus = new FakeBus();
        var handler = new ProcessFramesHandler(
            _devices, _telemetry, _stream, bus,
            new AdminNotices(directory, bus, Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminNotices>.Instance),
            new RecentControlSources(_time), _time);
        return (await handler.Handle(new ProcessFramesCommand(_device.Id, frames), CancellationToken.None)).Value;
    }

    [Fact]
    public async Task Set_on_a_sleepy_device_is_kept_pending_and_streamed_without_touching_the_link()
    {
        var result = await SetAsync("3");

        Assert.True(result.IsSuccess);
        Assert.Empty(_links.Sets);
        Assert.Equal("""{"heating":3}""", _telemetry.States[_device.Id].PendingSettings);
        var message = Assert.Single(_stream.Messages);
        Assert.Equal("changed", message.Type);
        Assert.Contains("pending_settings", JsonSerializer.Serialize(message.Data), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_later_set_replaces_the_pending_value_without_a_queue()
    {
        await SetAsync("1");
        await SetAsync("3");

        Assert.Equal("""{"heating":3}""", _telemetry.States[_device.Id].PendingSettings);
    }

    [Fact]
    public async Task Report_answers_with_the_pending_settings_and_keeps_them()
    {
        await SetAsync("3");

        var first = await ReportAsync(new DeviceFrame("status"));
        var second = await ReportAsync(new DeviceFrame("status"));

        Assert.Equal(3, first.Settings!.Value.GetProperty("heating").GetInt32());
        Assert.Equal(3, second.Settings!.Value.GetProperty("heating").GetInt32());
        Assert.NotNull(_telemetry.States[_device.Id].PendingSettings);
    }

    [Fact]
    public async Task A_state_frame_with_the_applied_value_clears_the_setting()
    {
        await SetAsync("3");

        var answer = await ReportAsync(new DeviceFrame("state", Values: Json("""{ "heating": 3 }""")));

        Assert.Null(answer.Settings);
        Assert.Null(_telemetry.States[_device.Id].PendingSettings);
    }

    [Fact]
    public async Task A_state_frame_with_the_old_value_keeps_the_setting_pending()
    {
        await SetAsync("3");

        var answer = await ReportAsync(new DeviceFrame("state", Values: Json("""{ "heating": 1 }""")));

        Assert.Equal(3, answer.Settings!.Value.GetProperty("heating").GetInt32());
    }

    [Theory]
    [InlineData("3.0")]
    [InlineData("3")]
    public async Task A_reported_number_equal_in_value_clears_the_setting_whatever_its_spelling(string reported)
    {
        await SetAsync("3");

        var answer = await ReportAsync(new DeviceFrame("state", Values: Json($$"""{ "heating": {{reported}} }""")));

        Assert.Null(answer.Settings);
    }

    [Fact]
    public async Task A_device_removed_while_a_change_waits_takes_no_more_settings_and_reports_are_refused()
    {
        await SetAsync("3");
        _device.Disable(Now);

        var set = await SetAsync("2");
        var report = await new ProcessFramesHandler(
            _devices, _telemetry, _stream, new FakeBus(),
            new AdminNotices(new FakeDirectory(), new FakeBus(), Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminNotices>.Instance),
            new RecentControlSources(_time), _time).Handle(new ProcessFramesCommand(_device.Id, [new DeviceFrame("status")]), CancellationToken.None);

        Assert.True(set.IsFailed);
        Assert.True(report.IsFailed);
        Assert.Equal("""{"heating":3}""", _telemetry.States[_device.Id].PendingSettings);
    }
}
