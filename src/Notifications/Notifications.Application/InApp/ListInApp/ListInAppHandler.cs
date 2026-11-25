using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;
using Notifications.Domain.InApp;

namespace Notifications.Application.InApp.ListInApp;

internal sealed class ListInAppHandler(IInAppNotificationRepository notifications, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<ListInAppQuery, NotificationListDto>
{
    public const int MaxLimit = 200;

    public async Task<Result<NotificationListDto>> Handle(ListInAppQuery query, CancellationToken cancellationToken)
    {
        var since = time.GetUtcNow() - InAppNotification.Retention;
        var limit = Math.Clamp(query.Limit, 1, MaxLimit);
        var items = await notifications.ListAsync(currentUser.RequiredUserId, since, query.UnreadOnly, limit, cancellationToken);
        var unread = await notifications.UnreadCountAsync(currentUser.RequiredUserId, since, cancellationToken);
        return new NotificationListDto(
            unread,
            [.. items.Select(n => new InAppNotificationView(n.Id, n.Source, n.Category, n.Tag, n.Title, n.Body, n.Url, n.HouseholdId, n.CreatedAt, n.ReadAt))]);
    }
}
