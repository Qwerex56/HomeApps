using Auth.Domain.Common;
using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Identities.Events;

/// <summary>Internal: sends the confirmation link to the new address (A-13).</summary>
public sealed record EmailChangeRequested(Guid IdentityId, Email NewEmail, string RawToken) : IDomainEvent;
