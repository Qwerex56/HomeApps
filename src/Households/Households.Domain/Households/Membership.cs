namespace Households.Domain.Households;

/// <summary>
/// One person in one household, inside the <see cref="Household"/> aggregate. Changed only through the aggregate.
/// Whether the account is active comes from <see cref="Users.UserCopy"/>, not from here (D6).
/// </summary>
public sealed class Membership
{
    private Membership() { }

    private Membership(Guid householdId, Guid userId, HouseholdRole role, Guid? invitedBy, DateTimeOffset? accessUntil, DateTimeOffset joinedAt)
    {
        HouseholdId = householdId;
        UserId = userId;
        Role = role;
        InvitedBy = invitedBy;
        AccessUntil = accessUntil;
        JoinedAt = joinedAt;
    }

    public Guid HouseholdId { get; private set; }

    public Guid UserId { get; private set; }

    public HouseholdRole Role { get; private set; }

    public Nickname? Nickname { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    /// <summary>D3c: who made the invite this person used; null for the creator, the guest link, or an erased inviter.</summary>
    public Guid? InvitedBy { get; private set; }

    /// <summary>D2c: guests only; after this moment the guest has no access and is removed.</summary>
    public DateTimeOffset? AccessUntil { get; private set; }

    public bool HasAccess(DateTimeOffset now) => AccessUntil is null || now < AccessUntil;

    internal static Membership Create(
        Guid householdId, Guid userId, HouseholdRole role, Guid? invitedBy, DateTimeOffset? accessUntil, DateTimeOffset joinedAt) =>
        new(householdId, userId, role, invitedBy, role == HouseholdRole.Guest ? accessUntil : null, joinedAt);

    internal void ChangeRole(HouseholdRole role)
    {
        Role = role;
        if (role != HouseholdRole.Guest)
        {
            AccessUntil = null;
        }
    }

    internal void SetNickname(Nickname? nickname) => Nickname = nickname;

    internal void SetAccessUntil(DateTimeOffset? until) => AccessUntil = until;

    internal void ForgetInviter() => InvitedBy = null;
}
