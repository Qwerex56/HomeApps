namespace Auth.Application.ConnectedApps;

/// <summary>The OAuth token response (RFC 6749 5.1) for a connected app.</summary>
public sealed record AppTokens(string AccessToken, int ExpiresIn, string RefreshToken, string Scope)
{
    /// <summary>One scope in v1: everything the person may do, minus what assistants may never do (JV13).</summary>
    public const string HomeAppsScope = "homeapps";
}
