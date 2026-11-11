using Auth.Domain.Common;
using FluentResults;

namespace Auth.Domain.ConnectedApps;

/// <summary>
/// An app that registered itself (OAuth dynamic client registration, RFC 7591), e.g. Claude Code or claude.ai. Public:
/// it has no secret, PKCE proves that the code goes back to whoever asked for it. Removed after 30 days unused.
/// </summary>
public sealed class AppClient
{
    public const string IdPrefix = "app_";
    public const int NameMaxLength = 100;
    public const int MaxRedirectUris = 5;
    public const int RedirectUriMaxLength = 512;

    private List<string> _redirectUris = [];

    private AppClient() { }

    private AppClient(string id, string name, List<string> redirectUris, DateTimeOffset registeredAt)
    {
        Id = id;
        Name = name;
        _redirectUris = redirectUris;
        RegisteredAt = registeredAt;
    }

    /// <summary>The OAuth <c>client_id</c>.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>What the app calls itself; shown on the consent screen as the app's own claim, not as a verified name.</summary>
    public string Name { get; private set; } = null!;

    public IReadOnlyList<string> RedirectUris => _redirectUris;

    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>The last time it got tokens; with <see cref="RegisteredAt"/> it decides when an unused client goes.</summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    public static Result<AppClient> Register(string? name, IReadOnlyCollection<string>? redirectUris, DateTimeOffset now)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > NameMaxLength)
        {
            return Result.Fail<AppClient>(ConnectedAppErrors.InvalidClientMetadata(
                $"client_name is required and may have at most {NameMaxLength} characters."));
        }

        if (redirectUris is null || redirectUris.Count == 0 || redirectUris.Count > MaxRedirectUris)
        {
            return Result.Fail<AppClient>(ConnectedAppErrors.InvalidRedirectUri(
                $"Between 1 and {MaxRedirectUris} redirect_uris are required."));
        }

        var invalid = redirectUris.Any(u => u is not { Length: <= RedirectUriMaxLength } || !RedirectUri.IsAcceptable(u));
        if (invalid)
        {
            return Result.Fail<AppClient>(ConnectedAppErrors.InvalidRedirectUri(
                "A redirect URI must be an absolute https address, or http on localhost, without a fragment."));
        }

        var (raw, _) = OpaqueToken.New();
        return new AppClient(IdPrefix + raw[..22], trimmed, [.. redirectUris.Distinct(StringComparer.Ordinal)], now);
    }

    public bool AllowsRedirect(string? redirectUri) =>
        redirectUri is not null && _redirectUris.Exists(registered => RedirectUri.Matches(registered, redirectUri));

    public void MarkUsed(DateTimeOffset now) => LastUsedAt = now;
}
