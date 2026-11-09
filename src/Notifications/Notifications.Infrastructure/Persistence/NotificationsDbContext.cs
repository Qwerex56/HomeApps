using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Recipients;

namespace Notifications.Infrastructure.Persistence;

/// <summary>Notifications' own database: recipients with their devices, mutes and settings.</summary>
public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, IPublisher publisher)
    : HomeAppsDbContext(options, publisher)
{
    public DbSet<Recipient> Recipients => Set<Recipient>();
}
