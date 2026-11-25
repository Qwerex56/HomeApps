using Notifications.Domain.InApp;

namespace Notifications.Application.Abstractions;

/// <summary>The notification center (2.13). Reads only look at entries created at or after <c>since</c> (the 30 days of retention).</summary>
public interface IInAppNotificationRepository
{
    Task<IReadOnlyList<InAppNotification>> ListAsync(Guid userId, DateTimeOffset since, bool unreadOnly, int limit, CancellationToken cancellationToken);

    Task<int> UnreadCountAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken);

    Task<InAppNotification?> FindAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<InAppNotification>> UnreadAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken);

    Task<IReadOnlyList<InAppNotification>> UnreadWithTagAsync(Guid userId, string tag, CancellationToken cancellationToken);

    void Add(InAppNotification notification);

    void Remove(InAppNotification notification);

    /// <summary>"Forget me": everything of the person goes.</summary>
    Task RemoveAllOfAsync(Guid userId, CancellationToken cancellationToken);
}
