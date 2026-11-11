namespace Auth.Domain.Identities;

/// <summary>
/// A refresh token just issued. <see cref="Raw"/> goes to the client once and is never stored.
/// <see cref="AuthenticatedAt"/> is when the session started, for the <c>auth_time</c> claim (R8).
/// <see cref="Audience"/> is set for a connected app's session (<see cref="Session.Audience"/>).
/// </summary>
public sealed record IssuedRefreshToken(
    string Raw, Guid SessionId, DateTimeOffset ExpiresAt, DateTimeOffset AuthenticatedAt, string? Audience = null);

/// <summary>Result of a successful registration: the new identity and the first session's refresh token.</summary>
public sealed record Registration(Identity Identity, IssuedRefreshToken RefreshToken);
