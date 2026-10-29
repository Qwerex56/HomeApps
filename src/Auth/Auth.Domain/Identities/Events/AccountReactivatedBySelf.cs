using Auth.Domain.Common;
using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Identities.Events;

/// <summary>Internal: emails "your account was restored", in case it was not this person (A-09).</summary>
public sealed record AccountReactivatedBySelf(Guid IdentityId, Email Email) : IDomainEvent;
