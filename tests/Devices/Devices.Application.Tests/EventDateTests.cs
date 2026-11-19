using Devices.Application.Common;
using Devices.Application.Frames;
using Devices.Application.Frames.ProcessFrames;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Devices;

namespace Devices.Application.Tests;

/// <summary>H-4c item 5: a device's own event date is believed only inside the retention window (not future, not older than 366 days).</summary>
public sealed class EventDateTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTelemetry _telemetry = new();
    private readonly FakeStream _stream = new();
    private readonly FakeBus _bus = new();
    private readonly FakeDirectory _directory = new();
    private readonly FakeTime _time = new(Now);
    private readonly Device _device;
    private readonly ProcessFramesHandler _handler;

    public EventDateTests()
    {
        var devices = new FakeDevices();
        _device = PairedDevice.Create(Now);
        devices.Add(_device);
        _handler = new ProcessFramesHandler(devices, _telemetry, _stream, _bus,
            new AdminNotices(_directory, _bus, Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminNotices>.Instance), new RecentControlSources(_time), _time);
    }

    private async Task<DateTimeOffset> StoredAsync(string? at)
    {
        await _handler.Handle(new ProcessFramesCommand(_device.Id, [new DeviceFrame("event", Name: "button", At: at)]), CancellationToken.None);
        return Assert.Single(_telemetry.Events).OccurredAt;
    }

    [Fact]
    public async Task An_event_from_1970_is_stored_at_the_time_of_receipt() =>
        Assert.Equal(Now, await StoredAsync("1970-01-01T00:00:00Z"));

    [Fact]
    public async Task An_event_two_days_old_keeps_its_time() =>
        Assert.Equal(Now.AddDays(-2), await StoredAsync(Now.AddDays(-2).ToString("O", System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public async Task An_event_400_days_old_is_stored_at_the_time_of_receipt() =>
        Assert.Equal(Now, await StoredAsync(Now.AddDays(-400).ToString("O", System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public async Task An_event_just_inside_366_days_keeps_its_time() =>
        Assert.Equal(Now.AddDays(-366), await StoredAsync(Now.AddDays(-366).ToString("O", System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public async Task An_event_just_past_366_days_is_stored_at_the_time_of_receipt() =>
        Assert.Equal(Now, await StoredAsync(Now.AddDays(-366).AddSeconds(-1).ToString("O", System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public async Task An_event_from_the_future_is_stored_at_the_time_of_receipt() =>
        Assert.Equal(Now, await StoredAsync(Now.AddMinutes(5).ToString("O", System.Globalization.CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData(null)]
    [InlineData("not a date")]
    public async Task A_missing_or_unreadable_time_is_the_time_of_receipt(string? at) =>
        Assert.Equal(Now, await StoredAsync(at));
}
