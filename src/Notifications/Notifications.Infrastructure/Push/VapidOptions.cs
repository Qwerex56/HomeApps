namespace Notifications.Infrastructure.Push;

/// <summary>
/// The instance's VAPID keys (RFC 8292): push services accept messages only from the server whose public key the
/// browser subscribed with. Make them once with <c>scripts/generate-vapid-keys.sh</c> and never change them, or every
/// device has to turn notifications on again.
/// </summary>
public sealed class VapidOptions
{
    public const string SectionName = "Notifications:Vapid";

    /// <summary>Uncompressed P-256 public key (65 bytes), base64url.</summary>
    public string? PublicKey { get; set; }

    /// <summary>P-256 private key (32 bytes), base64url. A secret.</summary>
    public string? PrivateKey { get; set; }

    /// <summary>How push services can reach the instance's admin: <c>mailto:</c> or an https address.</summary>
    public string Subject { get; set; } = "mailto:admin@homeapps.local";
}
