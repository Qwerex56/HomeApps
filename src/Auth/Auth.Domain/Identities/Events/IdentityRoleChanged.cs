using Auth.Domain.Common;
using HomeApps.BuildingBlocks.Domain;

namespace Auth.Domain.Identities.Events;

/// <summary>The Owner changed the server role (A-23). Becomes <c>HomeApps.Contracts.IdentityRoleChanged</c>.</summary>
public sealed record IdentityRoleChanged(Guid IdentityId, ServerRole Role) : IDomainEvent;
