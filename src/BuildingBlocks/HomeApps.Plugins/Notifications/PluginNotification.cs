using HomeApps.Contracts.Notifications;

namespace HomeApps.Plugins.Notifications;

/// <summary>
/// What a plugin wants to tell people. The fields mean the same as in <see cref="NotificationRequested"/>; the source is
/// the plugin's id. Nobody in <see cref="UserIds"/> is checked against the household: leave out the author and anyone
/// who must not know.
/// </summary>
/// <param name="Url">A page of the web app, e.g. <c>/homes/{householdId}/apps/chat</c>.</param>
public sealed record PluginNotification(
    IReadOnlyList<Guid> UserIds,
    string Category,
    string Title,
    string Body,
    string Url,
    Guid? HouseholdId = null,
    string? Tag = null,
    string? BodyWhenHidden = null,
    NotificationUrgency Urgency = NotificationUrgency.Normal);
