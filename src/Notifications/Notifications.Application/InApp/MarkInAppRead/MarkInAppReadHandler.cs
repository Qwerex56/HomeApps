using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;
using Notifications.Domain.InApp;

namespace Notifications.Application.InApp.MarkInAppRead;

internal sealed class MarkInAppReadHandler(IInAppNotificationRepository notifications, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<MarkInAppReadCommand>
{
    public async Task<Result> Handle(MarkInAppReadCommand command, CancellationToken cancellationToken)
    {
        var notification = await notifications.FindAsync(currentUser.RequiredUserId, command.Id, cancellationToken);
        if (notification is null)
        {
            return Result.Fail(InAppErrors.NotFound);
        }

        notification.MarkRead(time.GetUtcNow());
        return Result.Ok();
    }
}
