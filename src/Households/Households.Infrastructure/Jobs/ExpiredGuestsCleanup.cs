using Households.Application.Members;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Households.Infrastructure.Jobs;

/// <summary>
/// D2c: once an hour, removes guests whose access ended (they already lost access at that moment; this makes it
/// visible to everyone and publishes <c>MemberLeft(Expired)</c>). A failed round is logged and tried again next hour.
/// </summary>
internal sealed partial class ExpiredGuestsCleanup(IServiceScopeFactory scopes, ILogger<ExpiredGuestsCleanup> logger) : BackgroundService
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
            var removed = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RemoveExpiredGuestsCommand(), cancellationToken);
            if (removed.IsSuccess && removed.Value > 0)
            {
                LogRemoved(removed.Value);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Removed {Count} guests whose access ended.")]
    private partial void LogRemoved(int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Removing expired guests failed; trying again in an hour.")]
    private partial void LogFailed(Exception exception);
}
