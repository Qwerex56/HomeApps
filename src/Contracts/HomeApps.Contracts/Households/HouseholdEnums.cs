namespace HomeApps.Contracts.Households;

/// <summary>A person's role in one household (household.md 3). Ordered: a higher value is a higher role. Never in the JWT (D9).</summary>
public enum HouseholdRole
{
    Guest = 0,
    Member = 1,
    Admin = 2,
    Owner = 3,
}

public enum MemberLeftReason
{
    Left = 0,
    Removed = 1,

    /// <summary>The account was erased in Accounts.</summary>
    Erased = 2,

    /// <summary>A guest's access ended (D2c).</summary>
    Expired = 3,
}

/// <summary>D5: anything but <see cref="HandedOver"/> means the new Owner did not choose it and is told by mail (D5a).</summary>
public enum OwnershipTransferReason
{
    HandedOver = 0,
    OwnerBanned = 1,
    OwnerErased = 2,
}
