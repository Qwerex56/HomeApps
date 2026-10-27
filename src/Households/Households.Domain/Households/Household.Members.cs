using Households.Domain.Common;
using Households.Domain.Invites;
using Households.Domain.Households.Events;
using FluentResults;

namespace Households.Domain.Households;

/// <summary>Joining, leaving, removing, roles, nicknames and guest access (M1–M6, D2, D10).</summary>
public sealed partial class Household
{
    /// <summary>
    /// I5 and the guest link: a person joins as <see cref="HouseholdRole.Member"/> or <see cref="HouseholdRole.Guest"/>
    /// (D3a: never higher). A guest whose access already ended is replaced by the new membership.
    /// </summary>
    internal Result Join(Guid userId, HouseholdRole role, Guid? invitedBy, DateTimeOffset? accessUntil, int maxMembers, DateTimeOffset now)
    {
        if (role is not (HouseholdRole.Member or HouseholdRole.Guest))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "An invite gives at most the Member role (D3a).");
        }

        if (Status == HouseholdStatus.Archived)
        {
            return Result.Fail(HouseholdErrors.NotFound);
        }

        var existing = MemberOf(userId);
        if (existing is not null)
        {
            if (existing.HasAccess(now))
            {
                return Result.Fail(HouseholdErrors.AlreadyMember);
            }

            RemoveMembership(existing, MemberLeftReason.Expired);
        }

        if (_members.Count >= maxMembers)
        {
            return Result.Fail(HouseholdErrors.Full);
        }

        _members.Add(Membership.Create(Id, userId, role, invitedBy, accessUntil, now));
        Touch(now);
        _domainEvents.Add(new MemberJoined(Id, userId, role, invitedBy));
        return Result.Ok();
    }

    /// <summary>D2b: joining as a guest through the household's permanent guest link.</summary>
    public Result JoinAsGuest(Guid userId, InviteCode code, int maxMembers, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(time);

        if (GuestCode is null || GuestCode != code || Status == HouseholdStatus.Archived)
        {
            return Result.Fail(InviteErrors.NotFound);
        }

        return Join(userId, HouseholdRole.Guest, null, null, maxMembers, time.GetUtcNow());
    }

    /// <summary>M2: the Owner removes anyone else; an Admin removes members and guests only.</summary>
    public Result RemoveMember(Actor actor, Guid userId, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Admin);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        var target = MemberOf(userId);
        if (target is null)
        {
            return Result.Fail(HouseholdErrors.NotMember);
        }

        if (target.UserId == actor.Id || target.Role >= acting.Value.Role)
        {
            return Result.Fail(HouseholdErrors.Forbidden);
        }

        RemoveMembership(target, MemberLeftReason.Removed);
        Touch(now);
        return Result.Ok();
    }

    /// <summary>M3: anyone but the Owner leaves; the Owner hands the household over first (D5).</summary>
    public Result Leave(Actor actor, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Guest);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        if (acting.Value.Role == HouseholdRole.Owner)
        {
            return Result.Fail(HouseholdErrors.OwnerCannotLeave);
        }

        RemoveMembership(acting.Value, MemberLeftReason.Left);
        Touch(now);
        return Result.Ok();
    }

    /// <summary>M4: only the Owner, between Guest, Member and Admin (D2b, D3a: promotion only after joining).</summary>
    public Result ChangeRole(Actor actor, Guid userId, HouseholdRole role, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Owner);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        if (role is not (HouseholdRole.Guest or HouseholdRole.Member or HouseholdRole.Admin))
        {
            return Result.Fail(HouseholdErrors.RoleInvalid);
        }

        var target = MemberOf(userId);
        if (target is null)
        {
            return Result.Fail(HouseholdErrors.NotMember);
        }

        if (target.Role == HouseholdRole.Owner)
        {
            return Result.Fail(HouseholdErrors.Forbidden);
        }

        if (target.Role == role)
        {
            return Result.Fail(HouseholdErrors.RoleUnchanged);
        }

        var old = target.Role;
        target.ChangeRole(role);
        Touch(now);
        _domainEvents.Add(new MemberRoleChanged(Id, userId, old, role));
        return Result.Ok();
    }

    /// <summary>
    /// M6: members set their own nickname; a guest does not (D2a, read-only), the Owner or an Admin names the guest.
    /// A blank nickname clears it, so the display name from Accounts shows (Q4).
    /// </summary>
    public Result SetNickname(Actor actor, Guid userId, string? nickname, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Guest);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        var target = MemberOf(userId);
        if (target is null)
        {
            return Result.Fail(HouseholdErrors.NotMember);
        }

        var self = target.UserId == actor.Id;
        var allowed = self
            ? acting.Value.Role >= HouseholdRole.Member
            : acting.Value.Role >= HouseholdRole.Admin && target.Role == HouseholdRole.Guest;
        if (!allowed)
        {
            return Result.Fail(HouseholdErrors.Forbidden);
        }

        var valid = Nickname.Create(nickname);
        if (valid.IsFailed)
        {
            return valid.ToResult();
        }

        target.SetNickname(valid.Value);
        Touch(now);
        return Result.Ok();
    }

    /// <summary>D2c: the Owner or an Admin sets or clears (null) when a guest's access ends.</summary>
    public Result SetGuestAccess(Actor actor, Guid userId, DateTimeOffset? until, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Admin);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        var target = MemberOf(userId);
        if (target is null)
        {
            return Result.Fail(HouseholdErrors.NotMember);
        }

        if (target.Role != HouseholdRole.Guest)
        {
            return Result.Fail(HouseholdErrors.NotGuest);
        }

        if (until <= now)
        {
            return Result.Fail(HouseholdErrors.AccessUntilInvalid);
        }

        target.SetAccessUntil(until);
        Touch(now);
        return Result.Ok();
    }

    /// <summary>D2c: the hourly clean-up removes guests whose access ended. Returns how many left.</summary>
    public int RemoveExpiredGuests(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var expired = _members.Where(m => !m.HasAccess(now)).ToList();
        foreach (var guest in expired)
        {
            RemoveMembership(guest, MemberLeftReason.Expired);
        }

        if (expired.Count > 0)
        {
            Touch(now);
        }

        return expired.Count;
    }

    private void RemoveMembership(Membership membership, MemberLeftReason reason)
    {
        _members.Remove(membership);
        _domainEvents.Add(new MemberLeft(Id, membership.UserId, reason));
    }
}
