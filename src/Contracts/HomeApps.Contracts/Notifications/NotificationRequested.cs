namespace HomeApps.Contracts.Notifications;

/// <summary>
/// Someone should be told about something (docs/notifications.md). The Notifications service delivers it to every
/// device the people turned notifications on for, unless they muted it or are in their quiet hours. The sender decides
/// who gets it: Notifications does not check memberships, so leave out the person who caused it and anyone who must not
/// know. Plugins send it through <c>INotifier</c>, which fills in <see cref="Source"/>.
/// </summary>
/// <param name="UserIds">Who gets it.</param>
/// <param name="Source">Who sends it: a plugin id (<c>chat</c>) or a core service (<c>households</c>); people mute by it.</param>
/// <param name="Category">What kind of notification within <see cref="Source"/>, e.g. <c>message</c>.</param>
/// <param name="Title">Short, e.g. the conversation's name.</param>
/// <param name="Body">The content, e.g. "Ania: będę o 18". Plain text, no Markdown.</param>
/// <param name="Url">The app's page to open on a click, a path such as <c>/homes/{id}/apps/chat</c>.</param>
/// <param name="HouseholdId">The household it is about; people mute by it too.</param>
/// <param name="Tag">Notifications with the same tag replace each other on a device (one per conversation, not one per message).</param>
/// <param name="BodyWhenHidden">Shown instead of <see cref="Body"/> to people who hide content on the lock screen.</param>
public sealed record NotificationRequested(
    IReadOnlyList<Guid> UserIds,
    string Source,
    string Category,
    string Title,
    string Body,
    string Url,
    Guid? HouseholdId = null,
    string? Tag = null,
    string? BodyWhenHidden = null,
    NotificationUrgency Urgency = NotificationUrgency.Normal) : IIntegrationEvent;
