using Devices.Application.Housekeeping.PruneEvents;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devices.Infrastructure.Jobs;

/// <summary>D9, once a day: device events older than their household's retention go. A failed round is logged and repeated the next day.</summary>
internal sealed partial class EventPruning(IServiceScopeFactory scopes, ILogger<EventPruning> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var deleted = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new PruneEventsCommand(), cancellationToken);
            if (deleted.IsSuccess && deleted.Value > 0)
            {
                LogDeleted(deleted.Value);
            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Removed {Count} device events past their retention.")]
    private partial void LogDeleted(int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pruning device events failed; trying again tomorrow.")]
    private partial void LogFailed(Exception exception);
}
