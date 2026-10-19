namespace Auth.Domain.Identities;

/// <summary>
/// One link in a session's rotation chain (R6). Single use: using it marks it used and the session issues the next one.
/// Revocation lives on the <see cref="Session"/>, not on each token.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>D5: 30 days, extended on every rotation because each new token starts its own 30 days.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private RefreshToken() { }

    private RefreshToken(Guid id, Guid sessionId, string tokenHash, DateTimeOffset createdAt)
    {
        Id = id;
        SessionId = sessionId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + Lifetime;
    }

    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    /// <summary>SHA-256 of the raw token; the raw value is never stored.</summary>
    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;

    internal static RefreshToken Create(Guid sessionId, string tokenHash, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), sessionId, tokenHash, now);

    internal void MarkUsed(DateTimeOffset now) => UsedAt ??= now;
}
