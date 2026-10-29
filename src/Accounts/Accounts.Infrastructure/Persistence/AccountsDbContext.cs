using Accounts.Domain.Users;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Persistence;

/// <summary>Accounts' own database: users with their history, and the anonymous traces of erased accounts (plan 3.1).</summary>
public sealed class AccountsDbContext(DbContextOptions<AccountsDbContext> options, IPublisher publisher)
    : HomeAppsDbContext(options, publisher)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<HistoryEntry> History => Set<HistoryEntry>();

    public DbSet<ErasedUser> ErasedUsers => Set<ErasedUser>();
}
