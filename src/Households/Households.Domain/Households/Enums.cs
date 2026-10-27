namespace Households.Domain.Households;

/// <summary>
/// A person's role in one household (household.md 3). Ordered: a higher value is a higher role. Lives only in Households
/// and never reaches the JWT (D9). <see cref="Guest"/> is read-only (D2a).
/// </summary>
public enum HouseholdRole
{
    Guest = 0,
    Member = 1,
    Admin = 2,
    Owner = 3,
}

public enum HouseholdStatus
{
    Active = 0,

    /// <summary>D7: "deleted" for its members, kept for audit; only a server admin sees and restores it.</summary>
    Archived = 1,
}

/// <summary>Why someone is no longer a member.</summary>
public enum MemberLeftReason
{
    Left = 0,
    Removed = 1,

    /// <summary>The account was erased in Accounts ("forget me").</summary>
    Erased = 2,

    /// <summary>D2c: a guest's access ended.</summary>
    Expired = 3,
}

/// <summary>Why ownership moved (D5): handed over by the Owner, or automatically after a ban or an erase.</summary>
public enum OwnershipTransferReason
{
    HandedOver = 0,
    OwnerBanned = 1,
    OwnerErased = 2,
}
