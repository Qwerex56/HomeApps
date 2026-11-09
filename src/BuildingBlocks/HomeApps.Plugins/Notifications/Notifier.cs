using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Notifications;
using HomeApps.Plugins.Manifest;

namespace HomeApps.Plugins.Notifications;

internal sealed class Notifier(PluginManifest manifest, IEventBus bus) : INotifier
{
    public async Task NotifyAsync(PluginNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (manifest.FindNotificationCategory(notification.Category) is null)
        {
            throw new InvalidOperationException(
                $"Plugin '{manifest.Id}' sends notifications of category '{notification.Category}', which its manifest does not declare.");
        }

        var recipients = notification.UserIds.Distinct().ToList();
        if (recipients.Count == 0)
        {
            return;
        }

        await bus.PublishAsync(
            new NotificationRequested(
                recipients,
                manifest.Id,
                notification.Category,
                notification.Title,
                notification.Body,
                notification.Url,
                notification.HouseholdId,
                notification.Tag,
                notification.BodyWhenHidden,
                notification.Urgency),
            cancellationToken).ConfigureAwait(false);
    }
}
