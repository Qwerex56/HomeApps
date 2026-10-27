namespace HomeApps.Contracts.Households;

// Households publishes these so other services keep their own copy of who is in which household and with what role (D9).

/// <summary>A household exists; <see cref="OwnerId"/> is its first and only member.</summary>
public sealed record HouseholdCreated(Guid HouseholdId, Guid OwnerId, string Name) : IIntegrationEvent;

public sealed record HouseholdUpdated(Guid HouseholdId, string Name, string? Description) : IIntegrationEvent;

/// <summary>D7: the household is gone for its members (kept for audit). Treat its memberships as inactive.</summary>
public sealed record HouseholdArchived(Guid HouseholdId) : IIntegrationEvent;

/// <summary>A server admin brought an archived household back.</summary>
public sealed record HouseholdRestored(Guid HouseholdId) : IIntegrationEvent;

public sealed record MemberJoined(Guid HouseholdId, Guid UserId, HouseholdRole Role) : IIntegrationEvent;

public sealed record MemberLeft(Guid HouseholdId, Guid UserId, MemberLeftReason Reason) : IIntegrationEvent;

public sealed record MemberRoleChanged(Guid HouseholdId, Guid UserId, HouseholdRole OldRole, HouseholdRole NewRole) : IIntegrationEvent;

/// <summary>
/// The Owner changed: <see cref="FromUserId"/> is now an Admin (handed over) or a Member (banned), or gone (erased).
/// Carries <see cref="HouseholdName"/> so Auth can mail the new Owner without asking (D5a).
/// </summary>
public sealed record OwnershipTransferred(
    Guid HouseholdId, string HouseholdName, Guid FromUserId, Guid ToUserId, OwnershipTransferReason Reason) : IIntegrationEvent;
