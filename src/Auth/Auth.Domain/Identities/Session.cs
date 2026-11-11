using Auth.Domain.Common;

namespace Auth.Domain.Identities;

/// <summary>
/// One sign-in on one device: the refresh token family (R6, R7) and a row in "my sessions" (A-14).
/// Facts about the sign-in (client, method, when) live here once, not on every token.
/// </summary>
public sealed class Session
{
    private readonly List<RefreshToken> _refreshTokens = [];

    private Session() { }

    private Session(Guid id, Guid identityId, ClientInfo client, AuthMethod method, DateTimeOffset startedAt, string? audience)
    {
        Id = id;
        IdentityId = identityId;
        Client = client;
        Method = method;
        StartedAt = startedAt;
        Audience = audience;
    }

    public const int AudienceMaxLength = 256;

    public Guid Id { get; private set; }

    public Guid IdentityId { get; private set; }

    public ClientInfo Client { get; private set; } = null!;

    public AuthMethod Method { get; private set; }

    /// <summary>When the user actually authenticated; the <c>auth_time</c> claim (R8).</summary>
    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// A connected app's session (OAuth): the one resource its access tokens are for, e.g. the JARVIS gateway. Null for
    /// HomeApps' own clients, whose tokens are for the services.
    /// </summary>
    public string? Audience { get; private set; }

    public bool IsConnectedApp => Audience is not null;

    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens;

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && _refreshTokens.Any(t => t.IsUsable(now));

    /// <summary>When the session was last used: the newest token's creation (A-14). Derived, not stored.</summary>
    public DateTimeOffset LastUsedAt => _refreshTokens.Count == 0 ? StartedAt : _refreshTokens.Max(t => t.CreatedAt);

    internal RefreshToken? FindToken(string tokenHash) => _refreshTokens.Find(t => t.TokenHash == tokenHash);

    internal static (Session Session, IssuedRefreshToken Token) Start(
        Guid identityId, ClientInfo client, AuthMethod method, DateTimeOffset now, string? audience = null)
    {
        var session = new Session(Guid.CreateVersion7(now), identityId, client, method, now, audience);
        return (session, session.IssueToken(now));
    }

    internal IssuedRefreshToken IssueToken(DateTimeOffset now)
    {
        var (raw, hash) = OpaqueToken.New();
        var token = RefreshToken.Create(Id, hash, now);
        _refreshTokens.Add(token);
        return new IssuedRefreshToken(raw, Id, token.ExpiresAt, StartedAt, Audience);
    }

    internal void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
