using HomeMix.Application.Data.PurgeRemovedData;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeMix.Infrastructure.Jobs;

/// <summary>
/// Once an hour, deletes the data of add-ons uninstalled more than 30 days ago (docs/homemix.md, "Data"). A failed
/// round is logged and tried again next hour.
/// </summary>
internal sealed partial class RemovedDataCleanup(IServiceScopeFactory scopes, ILogger<RemovedDataCleanup> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

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
            var deleted = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new PurgeRemovedDataCommand(), cancellationToken);
            if (deleted.IsSuccess && deleted.Value > 0)
            {
                LogDeleted(deleted.Value);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} documents of add-ons uninstalled over 30 days ago.")]
    private partial void LogDeleted(int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Deleting old add-on data failed; trying again in an hour.")]
    private partial void LogFailed(Exception exception);
}
