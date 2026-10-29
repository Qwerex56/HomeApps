using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Identities.Events;

/// <summary>Becomes <c>HomeApps.Contracts.IdentitySignedIn</c>: Accounts sets LastSeenAt from it (A-35). Method is null for a refresh.</summary>
public sealed record IdentitySignedIn(Guid IdentityId, AuthMethod? Method, string ClientId, DateTimeOffset At) : IDomainEvent;
