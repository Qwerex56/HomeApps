using Devices.Application.Abstractions;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Logging;

namespace Devices.Application.Housekeeping.PruneEvents;

/// <summary>
/// Per household with events: ask Households for its retention (<c>eventDays</c>, and <c>locationEventDays</c> for events
/// with a location) and delete what is older. A household that cannot be asked keeps its events until the next day.
/// </summary>
internal sealed partial class PruneEventsHandler(IDeviceTelemetry telemetry, IHouseholdDirectory households, TimeProvider time, ILogger<PruneEventsHandler> logger)
    : ICommandHandler<PruneEventsCommand, int>
{
    public async Task<Result<int>> Handle(PruneEventsCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var deleted = 0;
        foreach (var householdId in await telemetry.HouseholdsWithEventsAsync(cancellationToken))
        {
            try
            {
                var retention = await households.GetEventRetentionAsync(householdId, cancellationToken);
                deleted += await telemetry.DeleteEventsAsync(
                    householdId, retention.CutoffFor(isLocation: false, now), retention.CutoffFor(isLocation: true, now), cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                LogRetentionUnavailable(householdId, exception);
            }
        }

        return deleted;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The event retention of household {HouseholdId} could not be read; its events are kept for now.")]
    private partial void LogRetentionUnavailable(Guid householdId, Exception exception);
}
