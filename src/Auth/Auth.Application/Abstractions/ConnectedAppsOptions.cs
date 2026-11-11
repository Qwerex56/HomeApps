namespace Auth.Application.Abstractions;

/// <summary>Section <c>Auth:ConnectedApps</c>: OAuth for apps that act for a person (jarvis-v1.md 3.1).</summary>
public sealed class ConnectedAppsOptions
{
    public const string SectionName = "Auth:ConnectedApps";

    /// <summary>
    /// The resources (RFC 8707) apps may get tokens for, e.g. <c>https://home-apps.dev/jarvis/mcp</c>. The token's
    /// <c>aud</c> is the resource, so it never works directly on the services. Empty: OAuth is off.
    /// </summary>
    public List<string> Resources { get; set; } = [];

    /// <summary>Registration is open to anyone (RFC 7591), so the number of registered apps is capped.</summary>
    public int MaxClients { get; set; } = 500;

    /// <summary>The configured resource matching <paramref name="requested"/> (a trailing slash does not matter).</summary>
    public string? FindResource(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            // OAuth 2.1 lets the client leave it out; with one resource there is nothing to choose.
            return Resources.Count == 1 ? Resources[0] : null;
        }

        return Resources.Find(r => string.Equals(r.TrimEnd('/'), requested.TrimEnd('/'), StringComparison.Ordinal));
    }
}
