using Households.Domain.Households;
using FluentResults;

namespace Households.Domain.Invites;

/// <summary>
/// One person's invite link to a household (D3, Discord-style): it has an author, a role (Member or Guest), works until
/// <see cref="ExpiresAt"/> and has no use limit (D3b). Created and revoked through <see cref="Household"/>, which knows
/// the caller's role. Usable only when not revoked <b>and</b> not expired.
/// </summary>
public sealed class Invite
{
    private Invite() { }

    private Invite(
        Guid id, Guid householdId, InviteCode code, HouseholdRole role, Guid createdBy, DateTimeOffset now, DateTimeOffset expiresAt,
        DateTimeOffset? guestAccessUntil)
    {
        Id = id;
        HouseholdId = householdId;
        Code = code;
        Role = role;
        CreatedBy = createdBy;
        CreatedAt = now;
        ExpiresAt = expiresAt;
        GuestAccessUntil = guestAccessUntil;
    }

    public Guid Id { get; private set; }

    public Guid HouseholdId { get; private set; }

    public InviteCode Code { get; private set; } = null!;

    /// <summary>D3a: <see cref="HouseholdRole.Member"/> or <see cref="HouseholdRole.Guest"/>, never higher.</summary>
    public HouseholdRole Role { get; private set; }

    /// <summary>The author; null after their account was erased (Q6), the invite keeps working.</summary>
    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>D2c: guest invites only; when the access of a guest who joined through it ends.</summary>
    public DateTimeOffset? GuestAccessUntil { get; private set; }

    /// <summary>D3c: how many joined through it; information only, there is no limit (D3b).</summary>
    public int Uses { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsUsable(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    internal static Invite Create(
        Guid householdId, Guid createdBy, HouseholdRole role, DateTimeOffset now, TimeSpan validFor, DateTimeOffset? guestAccessUntil) =>
        new(Guid.CreateVersion7(now), householdId, InviteCode.Generate(), role, createdBy, now, now + validFor, guestAccessUntil);

    internal Result Use(DateTimeOffset now)
    {
        if (!IsUsable(now))
        {
            return Result.Fail(InviteErrors.NotUsable);
        }

        Uses++;
        return Result.Ok();
    }

    /// <summary>
    /// Revokes without a permission check: for the system (household archived, D7; the author left or was removed, D3c).
    /// People revoke through <see cref="Household.RevokeInvite"/>. Revoking twice keeps the first time.
    /// </summary>
    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;

    /// <summary>Q6: the author's account was erased; the invite stays valid without a name.</summary>
    public void ForgetCreator() => CreatedBy = null;
}
