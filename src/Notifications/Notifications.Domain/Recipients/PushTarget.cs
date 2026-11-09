using FluentResults;

namespace Notifications.Domain.Recipients;

/// <summary>
/// Where and how to send a push to one browser, exactly as <c>PushManager.subscribe()</c> gave it: an https endpoint of
/// the push service and the two keys (RFC 8291) in base64url.
/// </summary>
public sealed record PushTarget(string Endpoint, string P256dh, string Auth)
{
    public const int MaxEndpointLength = 1000;

    public static Result<PushTarget> Create(string? endpoint, string? p256dh, string? auth)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Length > MaxEndpointLength
            || !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return Result.Fail(RecipientErrors.InvalidEndpoint);
        }

        if (DecodedLength(p256dh) != 65 || DecodedLength(auth) != 16)
        {
            return Result.Fail(RecipientErrors.InvalidKeys);
        }

        return new PushTarget(endpoint, p256dh!, auth!);
    }

    private static int DecodedLength(string? base64Url)
    {
        if (string.IsNullOrWhiteSpace(base64Url) || base64Url.Length > 200)
        {
            return -1;
        }

        var base64 = base64Url.Replace('-', '+').Replace('_', '/').TrimEnd('=');
        base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
        var buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64, buffer, out var written) ? written : -1;
    }
}
