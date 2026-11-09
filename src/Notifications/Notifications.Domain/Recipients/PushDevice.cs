namespace Notifications.Domain.Recipients;

/// <summary>
/// One browser that subscribed to Web Push, inside the <see cref="Recipient"/> aggregate. D6: only the User-Agent says
/// what it is; the endpoint and keys come from the browser and belong to it.
/// </summary>
public sealed class PushDevice
{
    private PushDevice() { }

    private PushDevice(PushTarget target, string? userAgent, DateTimeOffset now)
    {
        Id = Guid.CreateVersion7(now);
        Endpoint = target.Endpoint;
        P256dh = target.P256dh;
        Auth = target.Auth;
        UserAgent = Trim(userAgent);
        RegisteredAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>The push service's address for this browser (Apple, Google, Mozilla).</summary>
    public string Endpoint { get; private set; } = string.Empty;

    /// <summary>The browser's public key, base64url; the message is encrypted for it.</summary>
    public string P256dh { get; private set; } = string.Empty;

    /// <summary>The browser's authentication secret, base64url.</summary>
    public string Auth { get; private set; } = string.Empty;

    public string? UserAgent { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>When a notification last reached the push service for it.</summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    public PushTarget Target => new(Endpoint, P256dh, Auth);

    internal static PushDevice Create(PushTarget target, string? userAgent, DateTimeOffset now) => new(target, userAgent, now);

    internal void Renew(PushTarget target, string? userAgent, DateTimeOffset now)
    {
        P256dh = target.P256dh;
        Auth = target.Auth;
        UserAgent = Trim(userAgent) ?? UserAgent;
        RegisteredAt = now;
    }

    internal void MarkUsed(DateTimeOffset now) => LastUsedAt = now;

    private static string? Trim(string? userAgent) =>
        string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Length <= 512 ? userAgent : userAgent[..512];
}
