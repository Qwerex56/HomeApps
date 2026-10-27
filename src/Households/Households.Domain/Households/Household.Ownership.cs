using Households.Domain.Common;
using Households.Domain.Households.Events;
using FluentResults;

namespace Households.Domain.Households;

/// <summary>Handing the household over (M5) and the automatic transfer after a ban or an erase (D5).</summary>
public sealed partial class Household
{
    /// <summary>M5: the Owner hands over to a member or admin; the old Owner becomes an Admin.</summary>
    public Result TransferOwnership(Actor actor, Guid toUserId, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var acting = Acting(actor, now, HouseholdRole.Owner);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        var target = MemberOf(toUserId);
        if (target is null)
        {
            return Result.Fail(HouseholdErrors.NotMember);
        }

        if (target.Role is not (HouseholdRole.Member or HouseholdRole.Admin))
        {
            return Result.Fail(HouseholdErrors.TransferTargetInvalid);
        }

        acting.Value.ChangeRole(HouseholdRole.Admin);
        target.ChangeRole(HouseholdRole.Owner);
        OwnershipReceivedAt = null;
        Touch(now);
        _domainEvents.Add(new OwnershipTransferred(Id, actor.Id, toUserId, OwnershipTransferReason.HandedOver));
        return Result.Ok();
    }

    /// <summary>
    /// D5, D5b: an admin of the server disabled the Owner's account (a ban, not a self-disable). The oldest active Admin,
    /// else the oldest active Member, takes over and the banned Owner drops to Member, so an account taken over by
    /// someone else cannot come back as Owner. With nobody to take over, nothing changes.
    /// </summary>
    /// <param name="isActive">Whether a user's account is active, from the copy of Accounts (D6).</param>
    public void OwnerBanned(Guid userId, Func<Guid, bool> isActive, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(isActive);
        ArgumentNullException.ThrowIfNull(time);

        var owner = Owner;
        if (Status == HouseholdStatus.Archived || owner is null || owner.UserId != userId)
        {
            return;
        }

        var now = time.GetUtcNow();
        var successor = Successor(userId, isActive, now);
        if (successor is null)
        {
            return;
        }

        owner.ChangeRole(HouseholdRole.Member);
        HandOverAutomatically(userId, successor, OwnershipTransferReason.OwnerBanned, now);
    }

    /// <summary>
    /// S4 (UserErased): the person leaves every household they were in and their name disappears from "invited by" (Q6).
    /// If they owned an active household, D5 picks the new Owner; with nobody to take over, the household is archived.
    /// </summary>
    public void ForgetUser(Guid userId, Func<Guid, bool> isActive, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(isActive);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        foreach (var invited in _members.Where(m => m.InvitedBy == userId))
        {
            invited.ForgetInviter();
        }

        var membership = MemberOf(userId);
        if (membership is null)
        {
            return;
        }

        if (membership.Role == HouseholdRole.Owner && Status == HouseholdStatus.Active)
        {
            var successor = Successor(userId, isActive, now);
            if (successor is not null)
            {
                HandOverAutomatically(userId, successor, OwnershipTransferReason.OwnerErased, now);
            }
            else
            {
                ArchiveNow(now);
            }
        }

        RemoveMembership(membership, MemberLeftReason.Erased);
        Touch(now);
    }

    /// <summary>D5a: the new Owner saw the banner.</summary>
    public Result AcknowledgeOwnership(Actor actor, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(time);

        var acting = Acting(actor, time.GetUtcNow(), HouseholdRole.Owner);
        if (acting.IsFailed)
        {
            return acting.ToResult();
        }

        OwnershipReceivedAt = null;
        return Result.Ok();
    }

    /// <summary>D5: oldest active Admin, then oldest active Member; guests and inactive accounts never (D6).</summary>
    private Membership? Successor(Guid leavingOwnerId, Func<Guid, bool> isActive, DateTimeOffset now) =>
        _members
            .Where(m => m.UserId != leavingOwnerId && m.Role is HouseholdRole.Admin or HouseholdRole.Member)
            .Where(m => m.HasAccess(now) && isActive(m.UserId))
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .FirstOrDefault();

    private void HandOverAutomatically(Guid fromUserId, Membership successor, OwnershipTransferReason reason, DateTimeOffset now)
    {
        successor.ChangeRole(HouseholdRole.Owner);
        OwnershipReceivedAt = now;
        Touch(now);
        _domainEvents.Add(new OwnershipTransferred(Id, fromUserId, successor.UserId, reason));
    }
}
