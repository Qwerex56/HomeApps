using Auth.Domain.Common;
using FluentResults;

namespace Auth.Domain.ConnectedApps;

/// <summary>
/// What the consent screen hands to the app: one minute, single use, only for the same app, redirect URI and PKCE
/// verifier. Only the hash is stored. A code coming back a second time means it leaked: the session it started is
/// ended (OAuth 2.1, 4.1.3), so <see cref="SessionId"/> is kept after redemption.
/// </summary>
public sealed class AuthorizationCode
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

    private AuthorizationCode() { }

    private AuthorizationCode(
        Guid id, string codeHash, string clientId, Guid identityId, string redirectUri, string codeChallenge, string audience,
        DateTimeOffset now)
    {
        Id = id;
        CodeHash = codeHash;
        ClientId = clientId;
        IdentityId = identityId;
        RedirectUri = redirectUri;
        CodeChallenge = codeChallenge;
        Audience = audience;
        CreatedAt = now;
        ExpiresAt = now + Lifetime;
    }

    public Guid Id { get; private set; }

    public string CodeHash { get; private set; } = null!;

    public string ClientId { get; private set; } = null!;

    public Guid IdentityId { get; private set; }

    public string RedirectUri { get; private set; } = null!;

    public string CodeChallenge { get; private set; } = null!;

    /// <summary>The resource (RFC 8707) the person allowed; becomes the session's audience.</summary>
    public string Audience { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>The session the code started, to end it if the code comes back.</summary>
    public Guid? SessionId { get; private set; }

    /// <summary>The new code and its raw value, which goes to the app once in the redirect.</summary>
    public static (AuthorizationCode Code, string Raw) Issue(
        string clientId, Guid identityId, string redirectUri, string codeChallenge, string audience, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(codeChallenge);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        var (raw, hash) = OpaqueToken.New();
        return (new AuthorizationCode(Guid.CreateVersion7(now), hash, clientId, identityId, redirectUri, codeChallenge, audience, now), raw);
    }

    /// <summary>
    /// Every mismatch answers the same <c>invalid_grant</c>. A used code fails with <see cref="ConnectedAppErrors.CodeReused"/>
    /// so the handler can end <see cref="SessionId"/>.
    /// </summary>
    public Result Redeem(string clientId, string? redirectUri, string? codeVerifier, DateTimeOffset now)
    {
        if (UsedAt is not null)
        {
            return Result.Fail(ConnectedAppErrors.CodeReused);
        }

        if (ExpiresAt <= now
            || !string.Equals(ClientId, clientId, StringComparison.Ordinal)
            || !string.Equals(RedirectUri, redirectUri, StringComparison.Ordinal)
            || !Pkce.Verify(codeVerifier, CodeChallenge))
        {
            return Result.Fail(ConnectedAppErrors.InvalidGrant);
        }

        UsedAt = now;
        return Result.Ok();
    }

    public void StartedSession(Guid sessionId) => SessionId ??= sessionId;
}
