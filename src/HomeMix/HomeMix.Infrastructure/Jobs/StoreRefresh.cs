using HomeMix.Application.Store.RefreshStore;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeMix.Infrastructure.Jobs;

/// <summary>
/// Every 10 minutes, a round of the store's refresh (homemix-2.md 2.1): new official repositories of addons.json and
/// the entries due to be read again, a few per round. A failed round is logged and tried again in 10 minutes.
/// </summary>
internal sealed partial class StoreRefresh(IServiceScopeFactory scopes, ILogger<StoreRefresh> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

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
            var read = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RefreshStoreCommand(), cancellationToken);
            if (read.IsSuccess && read.Value > 0)
            {
                LogRead(read.Value);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Read {Count} store entries from GitHub.")]
    private partial void LogRead(int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refreshing the HomeMix store failed; trying again in 10 minutes.")]
    private partial void LogFailed(Exception exception);
}
