namespace HomeApps.Plugins.Manifest;

/// <summary>
/// A kind of notification the plugin sends (docs/notifications.md), so people's notification settings can name it
/// without knowing the plugin.
/// </summary>
/// <param name="Id">Lower-case, e.g. <c>message</c>; the <c>Category</c> of <c>PluginNotification</c>.</param>
public sealed record PluginNotificationCategory(string Id, LocalizedText Name);
