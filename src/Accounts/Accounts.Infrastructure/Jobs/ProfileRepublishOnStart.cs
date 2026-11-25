using Accounts.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accounts.Infrastructure.Jobs;

/// <summary>
/// Release 3.0, 2.11 (Decision 5): once, on the first start of 3.0, publishes <c>UserProfileUpdated</c> (now with the time
/// zone) for every account, so Households fills the zone of its user copies. A marker row keeps later starts quiet; the
/// events go to the outbox in the same transaction as the marker.
/// </summary>
internal sealed partial class ProfileRepublishOnStart(IServiceScopeFactory scopes, ILogger<ProfileRepublishOnStart> logger) : BackgroundService
{
    public const string MarkerName = "republish-profiles-3.0";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunOnceAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFailed(logger, ex);
        }
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        if (await db.StartupMarkers.AnyAsync(m => m.Name == MarkerName, cancellationToken))
        {
            return;
        }

        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var users = await db.Users.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var user in users)
        {
            var profile = user.Profile;
            await bus.PublishAsync(
                new UserProfileUpdated(user.Id, profile.DisplayName.Value, profile.AvatarUrl?.Value, profile.TimeZone.Value),
                cancellationToken);
        }

        db.StartupMarkers.Add(new StartupMarker(MarkerName, scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not republish the account profiles; it is tried again on the next start.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
