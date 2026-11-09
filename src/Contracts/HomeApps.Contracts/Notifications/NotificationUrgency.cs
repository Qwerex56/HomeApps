namespace HomeApps.Contracts.Notifications;

/// <summary>How soon a phone should wake up for it (the Web Push <c>Urgency</c> header).</summary>
public enum NotificationUrgency
{
    /// <summary>Delivered soon, but a phone saving battery may wait a little.</summary>
    Normal = 0,

    /// <summary>Delivered at once, e.g. a direct message.</summary>
    High = 1,
}
