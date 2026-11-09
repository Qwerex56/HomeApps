using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Notifications;
using Notifications.Application.Abstractions;
using Notifications.Domain.Recipients;

namespace Notifications.Application.Delivery.SendTest;

internal sealed class SendTestHandler(IRecipientRepository recipients, NotificationDispatcher dispatcher, ICurrentUser currentUser)
    : ICommandHandler<SendTestCommand, DeliveryReport>
{
    public const string Source = "notifications";

    public async Task<Result<DeliveryReport>> Handle(SendTestCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequiredUserId;
        var recipient = await recipients.FindAsync(userId, cancellationToken);
        if (recipient is null || recipient.Devices.Count == 0)
        {
            return Result.Fail(RecipientErrors.NoDevices);
        }

        var test = new NotificationRequested(
            [userId],
            Source,
            "test",
            "HomeApps",
            "Powiadomienia działają na tym urządzeniu.",
            "/account/notifications",
            Tag: "notifications-test",
            BodyWhenHidden: "Powiadomienie testowe",
            Urgency: NotificationUrgency.High);
        return await dispatcher.SendAsync(recipient, test, cancellationToken);
    }
}
