using Auth.Domain.Identities;
using Auth.Domain.Invitations;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

/// <summary>Auth's own database: identities with their sessions and logins, and invitations (plan 4.1).</summary>
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options, IPublisher publisher) : HomeAppsDbContext(options, publisher)
{
    public DbSet<Identity> Identities => Set<Identity>();

    public DbSet<Invitation> Invitations => Set<Invitation>();
}
