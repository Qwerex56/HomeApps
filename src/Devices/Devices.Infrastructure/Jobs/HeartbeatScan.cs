using Devices.Application.Heartbeat.CheckHeartbeats;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devices.Infrastructure.Jobs;

/// <summary>
/// Every 30 seconds: devices that owe a status report and sent none for 2.5 heartbeat periods become silent. Time before
/// this instance started does not count, so a restart does not make every device silent. A failed round is logged and repeated.
/// </summary>
internal sealed partial class HeartbeatScan(IServiceScopeFactory scopes, TimeProvider time, ILogger<HeartbeatScan> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startedAt = time.GetUtcNow();
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunOnceAsync(startedAt, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task RunOnceAsync(DateTimeOffset startedAt, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var silent = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CheckHeartbeatsCommand(startedAt), cancellationToken);
            if (silent.IsSuccess && silent.Value > 0)
            {
                LogSilent(silent.Value);
            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Count} devices stopped reporting.")]
    private partial void LogSilent(int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The heartbeat scan failed; trying again in 30 seconds.")]
    private partial void LogFailed(Exception exception);
}
