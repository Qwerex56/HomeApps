using Devices.Application.Housekeeping.PruneEvents;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Devices;

namespace Devices.Application.Tests;

public sealed class PruneEventsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 3, 0, 0, TimeSpan.Zero);

    private readonly FakeTelemetry _telemetry = new();
    private readonly FakeDirectory _directory = new();
    private readonly Guid _household = Guid.NewGuid();

    private Task<FluentResults.Result<int>> PruneAsync() =>
        new PruneEventsHandler(_telemetry, _directory, new FakeTime(Now), Microsoft.Extensions.Logging.Abstractions.NullLogger<PruneEventsHandler>.Instance)
            .Handle(new PruneEventsCommand(), CancellationToken.None);

    private void Add(int daysAgo, bool location, Guid? household = null) =>
        _telemetry.Add(DeviceEvent.Record(Guid.NewGuid(), household ?? _household, "e", null, Now.AddDays(-daysAgo), location));

    [Fact]
    public async Task Events_go_by_the_households_days_and_location_events_by_their_shorter_time()
    {
        _directory.Retention = new EventRetention(30, 7);
        Add(40, false);
        Add(20, false);
        Add(10, true);
        Add(3, true);

        var result = await PruneAsync();

        Assert.Equal(2, result.Value);
        Assert.Equal([20, 3], _telemetry.Events.Select(e => (int)(Now - e.OccurredAt).TotalDays));
    }

    [Fact]
    public async Task A_household_with_longer_retention_keeps_more()
    {
        _directory.Retention = new EventRetention(90, 30);
        Add(60, false);
        Add(60, true);

        await PruneAsync();

        Assert.Single(_telemetry.Events);
        Assert.False(_telemetry.Events[0].IsLocation);
    }

    [Fact]
    public async Task When_households_cannot_be_asked_nothing_is_deleted()
    {
        _directory.Down = true;
        Add(400, false);

        var result = await PruneAsync();

        Assert.Equal(0, result.Value);
        Assert.Single(_telemetry.Events);
    }
}
