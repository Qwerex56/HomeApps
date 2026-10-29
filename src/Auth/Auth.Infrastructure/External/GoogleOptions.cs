namespace Auth.Infrastructure.External;

/// <summary>Section <c>Auth:Google</c>. Without a client id Google sign-in answers <c>external_login.provider_disabled</c>.</summary>
public sealed class GoogleOptions
{
    public const string SectionName = "Auth:Google";

    public string? ClientId { get; set; }

    /// <summary>From a secret, never committed.</summary>
    public string? ClientSecret { get; set; }

    public Uri TokenEndpoint { get; set; } = new("https://oauth2.googleapis.com/token");

    public Uri MetadataAddress { get; set; } = new("https://accounts.google.com/.well-known/openid-configuration");

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
