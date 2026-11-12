using Microsoft.Net.Http.Headers;

namespace HomeMix.Api.Sandbox;

/// <summary>
/// The headers that make an add-on's files a sandbox (docs/homemix.md, "The sandbox", ADR 0009): CSP <c>sandbox</c>
/// gives an opaque origin even when the URL is opened in a tab, <c>connect-src</c> allows only the accepted origins,
/// and only HomeApps itself may frame it.
/// </summary>
internal static class SandboxHeaders
{
    public const string Sandbox = "sandbox allow-scripts allow-forms allow-popups";

    public static string ContentSecurityPolicy(IReadOnlyList<string> network) =>
        string.Join("; ",
            Sandbox,
            "default-src 'self' 'unsafe-inline' data: blob:",
            "script-src 'self' 'unsafe-inline' blob:",
            $"connect-src {(network.Count == 0 ? "'none'" : string.Join(' ', network))}",
            "form-action 'none'",
            "base-uri 'none'",
            "frame-ancestors 'self'");

    public static void Apply(IHeaderDictionary headers, IReadOnlyList<string> network)
    {
        headers[HeaderNames.ContentSecurityPolicy] = ContentSecurityPolicy(network);
        // The frame's origin is opaque ("null"), so its module scripts are cross-origin requests.
        headers[HeaderNames.AccessControlAllowOrigin] = "*";
        headers[HeaderNames.XContentTypeOptions] = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Cross-Origin-Resource-Policy"] = "cross-origin";
        // The address holds the installation and the content belongs to one package version; an update changes the
        // package, so revalidate instead of caching for long.
        headers[HeaderNames.CacheControl] = "no-cache";
    }
}
