using Households.Domain.Common;
using Households.Domain.Invites;
using FluentResults;

namespace Households.Domain.Households;

/// <summary>Invites (I1, I3, I5, D3) and the permanent guest link (D2b). The rules live here because they need the caller's role.</summary>
public sealed partial class Household
{
    /// <summary>
    /// I1, D3a: everybody but a guest makes their own invite, for <see cref="HouseholdRole.Member"/> or
    /// <see cref="HouseholdRole.Guest"/>. <paramref name="guestAccessUntil"/> (D2c) is only for guest invites.
    /// </summary>
    public Result<Invite> CreateInvite(
        Actor actor, HouseholdRole role, InviteValidity validity, DateTimeOffset? guestAccessUntil, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Member);
        if (acting.IsFailed)
        {
            return acting.ToResult<Invite>();
        }

        if (role is not (HouseholdRole.Member or HouseholdRole.Guest))
        {
            return Result.Fail<Invite>(InviteErrors.RoleInvalid);
        }

        if (!Enum.IsDefined(validity))
        {
            return Result.Fail<Invite>(InviteErrors.ValidityInvalid);
        }

        if (guestAccessUntil is not null && (role != HouseholdRole.Guest || guestAccessUntil <= now))
        {
            return Result.Fail<Invite>(HouseholdErrors.AccessUntilInvalid);
        }

        return Invite.Create(Id, actor.Id, role, now, validity.Duration(), guestAccessUntil);
    }

    /// <summary>I3: the Owner and Admins revoke any invite of the household, a member only their own (D3a).</summary>
    public Result RevokeInvite(Actor actor, Invite invite, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(invite);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Member);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        if (invite.HouseholdId != Id)
        {
            return Result.Fail(InviteErrors.NotFound);
        }

        if (acting.Value.Role < HouseholdRole.Admin && invite.CreatedBy != actor.Id)
        {
            return Result.Fail(HouseholdErrors.Forbidden);
        }

        invite.Revoke(now);
        return Result.Ok();
    }

    /// <summary>I2: whose invites the caller sees: everyone's (null) for the Owner and Admins, only their own for a member.</summary>
    public Result<Guid?> InvitesVisibleTo(Actor actor, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var acting = Acting(actor, time.GetUtcNow(), HouseholdRole.Member);
        if (acting.IsFailed)
        {
            return acting.ToResult<Guid?>();
        }

        return acting.Value.Role >= HouseholdRole.Admin ? (Guid?)null : actor.Id;
    }

    /// <summary>I5: the invite is used and the person joins with the invite's role (D3a) and guest access end (D2c), D10 limit.</summary>
    public Result Accept(Invite invite, Guid userId, int maxMembers, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(invite);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        if (invite.HouseholdId != Id || !invite.IsUsable(now))
        {
            return Result.Fail(InviteErrors.NotUsable);
        }

        var joined = Join(userId, invite.Role, invite.CreatedBy, invite.GuestAccessUntil, maxMembers, now);
        if (joined.IsFailed)
        {
            return joined;
        }

        return invite.Use(now);
    }

    /// <summary>D2b: a new guest link; the old one stops working. The Owner or an Admin.</summary>
    public Result<InviteCode> RegenerateGuestLink(Actor actor, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Admin);
        if (acting.IsFailed)
        {
            return acting.ToResult<InviteCode>();
        }

        GuestCode = InviteCode.Generate();
        Touch(now);
        return GuestCode;
    }

    /// <summary>D2b: turns the guest link off. The Owner or an Admin.</summary>
    public Result DisableGuestLink(Actor actor, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Admin);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        GuestCode = null;
        Touch(now);
        return Result.Ok();
    }
}
