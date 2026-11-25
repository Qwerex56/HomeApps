using HomeApps.BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HomeApps.BuildingBlocks.Infrastructure.Persistence;

internal sealed class AppliedMigrations<TContext>(TContext db) : IAppliedMigrations
    where TContext : HomeAppsDbContext
{
    public async Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken) =>
        (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
}
