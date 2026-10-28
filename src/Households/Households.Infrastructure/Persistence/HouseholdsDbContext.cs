using Households.Domain.Households;
using Households.Domain.Invites;
using Households.Domain.Users;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Households.Infrastructure.Persistence;

/// <summary>Households' own database: households with memberships, invites and the copy of accounts (plan, step 3).</summary>
public sealed class HouseholdsDbContext(DbContextOptions<HouseholdsDbContext> options, IPublisher publisher)
    : HomeAppsDbContext(options, publisher)
{
    public DbSet<Household> Households => Set<Household>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<Invite> Invites => Set<Invite>();

    public DbSet<UserCopy> Users => Set<UserCopy>();

    /// <summary>
    /// A change to memberships alone would not update the <c>Households</c> row, so its <c>xmin</c> would not guard the
    /// aggregate (two joins racing for the last place, D10). Every household whose memberships changed is written too.
    /// </summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var changed = ChangeTracker.Entries<Membership>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.HouseholdId)
            .ToHashSet();
        foreach (var household in ChangeTracker.Entries<Household>().Where(e => changed.Contains(e.Entity.Id)))
        {
            if (household.State == EntityState.Unchanged)
            {
                household.Property(h => h.UpdatedAt).IsModified = true;
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
