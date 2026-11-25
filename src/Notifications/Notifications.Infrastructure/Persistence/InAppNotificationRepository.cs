using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Domain.InApp;

namespace Notifications.Infrastructure.Persistence;

internal sealed class InAppNotificationRepository(NotificationsDbContext db) : IInAppNotificationRepository
{
    public async Task<IReadOnlyList<InAppNotification>> ListAsync(
        Guid userId, DateTimeOffset since, bool unreadOnly, int limit, CancellationToken cancellationToken) =>
        await db.InAppNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.CreatedAt >= since && (!unreadOnly || n.ReadAt == null))
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<int> UnreadCountAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken) =>
        db.InAppNotifications.CountAsync(n => n.UserId == userId && n.ReadAt == null && n.CreatedAt >= since, cancellationToken);

    public Task<InAppNotification?> FindAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        db.InAppNotifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<InAppNotification>> UnreadAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken) =>
        await db.InAppNotifications.Where(n => n.UserId == userId && n.ReadAt == null && n.CreatedAt >= since).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InAppNotification>> UnreadWithTagAsync(Guid userId, string tag, CancellationToken cancellationToken) =>
        await db.InAppNotifications.Where(n => n.UserId == userId && n.ReadAt == null && n.Tag == tag).ToListAsync(cancellationToken);

    public void Add(InAppNotification notification) => db.InAppNotifications.Add(notification);

    public void Remove(InAppNotification notification) => db.InAppNotifications.Remove(notification);

    public Task RemoveAllOfAsync(Guid userId, CancellationToken cancellationToken) =>
        db.InAppNotifications.Where(n => n.UserId == userId).ExecuteDeleteAsync(cancellationToken);
}
