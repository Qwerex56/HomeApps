using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Recipients;

namespace Notifications.Application.Delivery.Deliver;

internal sealed class DeliverNotificationHandler(IRecipientRepository recipients, NotificationDispatcher dispatcher, TimeProvider time)
    : ICommandHandler<DeliverNotificationCommand, DeliveryReport>
{
    public async Task<Result<DeliveryReport>> Handle(DeliverNotificationCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var userIds = request.UserIds.Distinct().ToList();
        var found = await recipients.GetManyAsync(userIds, cancellationToken);
        var report = new DeliveryReport(0, 0, 0, userIds.Count - found.Count);
        var now = time.GetUtcNow();

        foreach (var recipient in found)
        {
            report = recipient.Decide(request.HouseholdId, request.Source, now) == DeliveryDecision.Deliver
                ? report.Add(await dispatcher.SendAsync(recipient, request, cancellationToken))
                : report.Add(new DeliveryReport(0, 0, 0, 1));
        }

        return report;
    }
}
