using Auth.Domain.Identities;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Jobs;

/// <summary>
/// A-33: dead data out, so an <see cref="Identity"/> aggregate stays small. Plain set-based deletes: nothing here is
/// a business decision, every row it touches can no longer be used. Invitations need nothing, their status is derived.
/// </summary>
internal static class AuthCleanup
{
    /// <summary>How long dead sessions and tokens stay visible (for "my sessions" and reuse detection) before deletion.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    public static async Task<CleanupResult> RunAsync(AuthDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        var cutoff = now - Retention;
        var tokens = await db.Set<RefreshToken>()
            .Where(t => t.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        // A session has a token from its first moment, so one without tokens only had expired ones, now deleted.
        var sessions = await db.Set<Session>()
            .Where(s => s.RevokedAt < cutoff || (s.StartedAt < cutoff && !s.RefreshTokens.Any()))
            .ExecuteDeleteAsync(cancellationToken);

        var links = await db.Identities
            .Where(i => i.EmailToken != null && i.EmailToken.ExpiresAt < now)
            .ExecuteUpdateAsync(set => set.SetProperty(i => i.EmailToken, (EmailToken?)null), cancellationToken);

        return new CleanupResult(tokens, sessions, links);
    }
}

internal sealed record CleanupResult(int RefreshTokens, int Sessions, int EmailLinks);

/// <summary>Runs <see cref="AuthCleanup"/> at start and then once a day.</summary>
internal sealed partial class DailyCleanup(IServiceScopeFactory scopes, TimeProvider time, ILogger<DailyCleanup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1), time);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                var result = await AuthCleanup.RunAsync(db, time.GetUtcNow(), stoppingToken);
                LogCleaned(logger, result.RefreshTokens, result.Sessions, result.EmailLinks);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Cleanup is housekeeping: a failed run is retried tomorrow, it must not stop the service.
                LogFailed(logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Cleanup removed {RefreshTokens} refresh tokens, {Sessions} sessions and {EmailLinks} expired email links")]
    private static partial void LogCleaned(ILogger logger, int refreshTokens, int sessions, int emailLinks);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cleanup failed; it runs again tomorrow")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
