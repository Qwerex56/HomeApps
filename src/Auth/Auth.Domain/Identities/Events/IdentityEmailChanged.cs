using Auth.Domain.Common;
using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Identities.Events;

/// <summary>The new email was confirmed (A-13). Becomes <c>HomeApps.Contracts.IdentityEmailChanged</c>.</summary>
public sealed record IdentityEmailChanged(Guid IdentityId, Email Email) : IDomainEvent;
