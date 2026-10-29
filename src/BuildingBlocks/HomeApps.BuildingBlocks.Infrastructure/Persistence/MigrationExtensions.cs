using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HomeApps.BuildingBlocks.Infrastructure.Persistence;

public static class MigrationExtensions
{
    /// <summary>Setting that turns migrations on start on outside Development (release plan Q3: one instance per service).</summary>
    public const string MigrateOnStartupKey = "Database:MigrateOnStartup";

    /// <summary>Applies pending EF Core migrations. Call it on start in Development (and in integration tests).</summary>
    public static async Task MigrateDatabaseAsync<TContext>(this IHost host, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(host);

        await using var scope = host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Applies pending migrations when <see cref="ShouldMigrateOnStartup"/> says so: always in Development, elsewhere
    /// only with <c>Database:MigrateOnStartup=true</c> (set in compose.prod.yaml).
    /// </summary>
    public static async Task MigrateDatabaseOnStartupAsync<TContext>(this IHost host, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(host);

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var environment = host.Services.GetRequiredService<IHostEnvironment>();
        if (ShouldMigrateOnStartup(configuration, environment))
        {
            await host.MigrateDatabaseAsync<TContext>(cancellationToken);
        }
    }

    public static bool ShouldMigrateOnStartup(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        return configuration.GetValue<bool?>(MigrateOnStartupKey) ?? environment.IsDevelopment();
    }
}
