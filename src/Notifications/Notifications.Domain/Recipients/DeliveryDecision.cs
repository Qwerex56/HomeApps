namespace Notifications.Domain.Recipients;

/// <summary>What happens to one notification for one person.</summary>
public enum DeliveryDecision
{
    /// <summary>Sent to every device of the person.</summary>
    Deliver = 0,

    /// <summary>The person has no device with notifications on.</summary>
    NoDevices = 1,

    /// <summary>The household, or this plugin in it, is muted.</summary>
    Muted = 2,

    /// <summary>The person's quiet hours; the notification is dropped, not delayed (N3).</summary>
    QuietHours = 3,
}
