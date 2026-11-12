using Jarvis.Application.Housekeeping.ApplyRetention;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jarvis.Infrastructure.Jobs;

/// <summary>
/// JV12 every 6 hours: texts of the log older than 7 days and rows older than 30 go. A failed round is logged and
/// tried again next time.
/// </summary>
internal sealed partial class RetentionJob(IServiceScopeFactory scopes, ILogger<RetentionJob> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(6);

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
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ApplyRetentionCommand(), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Applying the log retention failed; trying again in 6 hours.")]
    private partial void LogFailed(Exception exception);
}
