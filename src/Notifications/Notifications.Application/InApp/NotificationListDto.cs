namespace Notifications.Application.InApp;

/// <summary>2.13: <see cref="UnreadCount"/> counts every unread of the last 30 days, not only the ones in <see cref="Items"/>.</summary>
public sealed record NotificationListDto(int UnreadCount, IReadOnlyList<InAppNotificationView> Items);
