using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HomeApps.BuildingBlocks.Infrastructure.Persistence;

public static class MigrationExtensions
{
    /// <summary>Applies pending EF Core migrations. Call it on start in Development (and in integration tests).</summary>
    public static async Task MigrateDatabaseAsync<TContext>(this IHost host, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(host);

        await using var scope = host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
