using HomeApps.Contracts.Notifications;

namespace Notifications.Application.Abstractions;

/// <summary>What the service worker gets (docs/notifications.md, "Payload"); the sender adds nothing but encryption.</summary>
public sealed record PushMessage(
    string Title,
    string Body,
    string Url,
    string Source,
    string Category,
    Guid? HouseholdId,
    string? Tag,
    NotificationUrgency Urgency);
