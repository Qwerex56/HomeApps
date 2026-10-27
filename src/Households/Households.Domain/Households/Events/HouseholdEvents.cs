using HomeApps.BuildingBlocks.Domain;

namespace Households.Domain.Households.Events;

// Domain events of the Household aggregate. Step 2 turns each into the HomeApps.Contracts.Households message of the
// same name (D9: other services keep their own copy of memberships from these).

public sealed record HouseholdCreated(Guid HouseholdId, Guid OwnerId, string Name) : IDomainEvent;

public sealed record HouseholdUpdated(Guid HouseholdId, string Name, string? Description) : IDomainEvent;

/// <summary>D7: the household is gone for its members. Step 2 revokes its invites when this is raised.</summary>
public sealed record HouseholdArchived(Guid HouseholdId) : IDomainEvent;

public sealed record HouseholdRestored(Guid HouseholdId) : IDomainEvent;

public sealed record MemberJoined(Guid HouseholdId, Guid UserId, HouseholdRole Role, Guid? InvitedBy) : IDomainEvent;

/// <summary>Step 2 revokes the person's own invites when they left, were removed or expired (D3c), but not when erased (Q6).</summary>
public sealed record MemberLeft(Guid HouseholdId, Guid UserId, MemberLeftReason Reason) : IDomainEvent;

public sealed record MemberRoleChanged(Guid HouseholdId, Guid UserId, HouseholdRole OldRole, HouseholdRole NewRole) : IDomainEvent;

/// <summary>D5: <see cref="Reason"/> other than <c>HandedOver</c> means the new Owner must be told (D5a).</summary>
public sealed record OwnershipTransferred(Guid HouseholdId, Guid FromUserId, Guid ToUserId, OwnershipTransferReason Reason) : IDomainEvent;
