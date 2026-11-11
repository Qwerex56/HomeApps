namespace Auth.Domain.ConnectedApps;

/// <summary>Where an app may receive its authorization code (OAuth 2.1, RFC 8252 for apps on the person's computer).</summary>
public static class RedirectUri
{
    /// <summary>Absolute, no fragment, and https, or http on a loopback host (Claude Code listens on localhost).</summary>
    public static bool IsAcceptable(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.Fragment) || value.Contains('#', StringComparison.Ordinal))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && IsLoopback(uri));
    }

    /// <summary>
    /// Exact match, except that on a loopback host any port is allowed (RFC 8252 7.3): an app on the person's computer
    /// picks a free port each time.
    /// </summary>
    public static bool Matches(string registered, string requested)
    {
        if (string.Equals(registered, requested, StringComparison.Ordinal))
        {
            return true;
        }

        if (!Uri.TryCreate(registered, UriKind.Absolute, out var a) || !Uri.TryCreate(requested, UriKind.Absolute, out var b))
        {
            return false;
        }

        return IsLoopback(a) && IsLoopback(b)
            && a.Scheme == b.Scheme
            && string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.PathAndQuery, b.PathAndQuery, StringComparison.Ordinal)
            && string.IsNullOrEmpty(b.Fragment);
    }

    private static bool IsLoopback(Uri uri) =>
        uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
}
