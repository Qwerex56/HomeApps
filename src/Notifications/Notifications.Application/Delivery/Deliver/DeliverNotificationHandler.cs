using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;
using Notifications.Domain.InApp;
using Notifications.Domain.Recipients;

namespace Notifications.Application.Delivery.Deliver;

internal sealed class DeliverNotificationHandler(
    IRecipientRepository recipients, IInAppNotificationRepository inApp, NotificationDispatcher dispatcher, TimeProvider time)
    : ICommandHandler<DeliverNotificationCommand, DeliveryReport>
{
    public async Task<Result<DeliveryReport>> Handle(DeliverNotificationCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var userIds = request.UserIds.Distinct().ToList();
        var found = await recipients.GetManyAsync(userIds, cancellationToken);
        var report = new DeliveryReport(0, 0, 0, userIds.Count - found.Count);
        var now = time.GetUtcNow();

        // 2.13: the notification center gets everyone who has not muted it, with or without a device or push.
        var byUser = found.ToDictionary(r => r.UserId);
        foreach (var userId in userIds)
        {
            if (byUser.TryGetValue(userId, out var known) && known.IsMuted(request.HouseholdId, request.Source))
            {
                continue;
            }

            if (request.Tag is not null)
            {
                foreach (var replaced in await inApp.UnreadWithTagAsync(userId, request.Tag, cancellationToken))
                {
                    inApp.Remove(replaced);
                }
            }

            inApp.Add(InAppNotification.Create(
                Guid.CreateVersion7(now), userId, request.Source, request.Category, request.Tag, request.Title, request.Body, request.Url,
                request.HouseholdId, now));
        }

        foreach (var recipient in found)
        {
            report = recipient.Decide(request.HouseholdId, request.Source, now) == DeliveryDecision.Deliver
                ? report.Add(await dispatcher.SendAsync(recipient, request, cancellationToken))
                : report.Add(new DeliveryReport(0, 0, 0, 1));
        }

        return report;
    }
}
