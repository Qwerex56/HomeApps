namespace Notifications.Application.Abstractions;

public enum PushOutcome
{
    /// <summary>The push service accepted it (it may still wait for the device to come online).</summary>
    Delivered = 0,

    /// <summary>404/410: the subscription is gone for good; the device is removed.</summary>
    Gone = 1,

    /// <summary>Anything else (push service down, timeout); the device stays and the next notification tries again.</summary>
    Failed = 2,
}
