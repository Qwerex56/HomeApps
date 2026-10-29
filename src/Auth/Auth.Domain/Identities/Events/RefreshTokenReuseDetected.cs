using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Identities.Events;

/// <summary>Internal: a used refresh token came back, or from another client (R7). The session was revoked; logged for audit.</summary>
public sealed record RefreshTokenReuseDetected(Guid IdentityId, Guid SessionId, string ClientId) : IDomainEvent;
