using Auth.Domain.ConnectedApps;
using Auth.Domain.Identities;
using Auth.Domain.Invitations;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

/// <summary>Auth's own database: identities with their sessions and logins, invitations (plan 4.1) and connected apps (jarvis-v1.md 3.1).</summary>
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options, IPublisher publisher) : HomeAppsDbContext(options, publisher)
{
    public DbSet<Identity> Identities => Set<Identity>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<AppClient> AppClients => Set<AppClient>();

    public DbSet<AuthorizationCode> AuthorizationCodes => Set<AuthorizationCode>();
}
