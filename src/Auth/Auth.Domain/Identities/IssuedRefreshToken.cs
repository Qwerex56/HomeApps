namespace Auth.Domain.Identities;

/// <summary>
/// A refresh token just issued. <see cref="Raw"/> goes to the client once and is never stored.
/// <see cref="AuthenticatedAt"/> is when the session started, for the <c>auth_time</c> claim (R8).
/// </summary>
public sealed record IssuedRefreshToken(string Raw, Guid SessionId, DateTimeOffset ExpiresAt, DateTimeOffset AuthenticatedAt);

/// <summary>Result of a successful registration: the new identity and the first session's refresh token.</summary>
public sealed record Registration(Identity Identity, IssuedRefreshToken RefreshToken);
