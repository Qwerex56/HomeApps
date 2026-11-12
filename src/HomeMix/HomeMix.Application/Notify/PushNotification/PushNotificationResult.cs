namespace HomeMix.Application.Notify.PushNotification;

/// <param name="Recipients">How many people it was sent to; each one's own settings still decide whether it shows.</param>
public sealed record PushNotificationResult(int Recipients);
