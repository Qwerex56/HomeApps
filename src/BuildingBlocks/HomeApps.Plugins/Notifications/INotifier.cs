namespace HomeApps.Plugins.Notifications;

/// <summary>
/// Sends a notification to people's devices through the Notifications service (docs/notifications.md). It goes through
/// the outbox, so it leaves only if the current command commits. Call it from a command handler or a domain event handler.
/// </summary>
public interface INotifier
{
    /// <summary>
    /// Publishes <see cref="PluginNotification"/> with the plugin's id as its source. The category must be one the
    /// manifest declares in <c>Notifications</c>; anything else is a bug and throws.
    /// </summary>
    Task NotifyAsync(PluginNotification notification, CancellationToken cancellationToken);
}
