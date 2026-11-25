using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;
using Notifications.Domain.InApp;

namespace Notifications.Application.InApp.MarkAllInAppRead;

internal sealed class MarkAllInAppReadHandler(IInAppNotificationRepository notifications, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<MarkAllInAppReadCommand>
{
    public async Task<Result> Handle(MarkAllInAppReadCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        foreach (var notification in await notifications.UnreadAsync(currentUser.RequiredUserId, now - InAppNotification.Retention, cancellationToken))
        {
            notification.MarkRead(now);
        }

        return Result.Ok();
    }
}
