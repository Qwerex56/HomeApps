using Households.Domain.Households;
using Households.Domain.Households.Events;
using Households.Domain.Invites;

namespace Households.Domain.Tests;

public sealed class InviteTests
{
    private readonly FixedTime _time = new(A.Now);

    [Theory]
    [InlineData("owner", true)]
    [InlineData("admin", true)]
    [InlineData("member", true)]
    [InlineData("guest", false)]
    public void D3a_EverybodyButAGuestInvites(string who, bool allowed)
    {
        var h = new AHousehold(_time);

        var result = h.Household.CreateInvite(h.Of(who), HouseholdRole.Member, InviteValidity.SevenDays, null, _time);

        Assert.Equal(allowed, result.IsSuccess);
        if (allowed)
        {
            var invite = result.Value;
            Assert.Equal((h.Household.Id, h.Of(who).Id, HouseholdRole.Member), (invite.HouseholdId, invite.CreatedBy, invite.Role));
            Assert.Equal(_time.Now.AddDays(7), invite.ExpiresAt);
            Assert.Equal(0, invite.Uses);
        }
        else
        {
            Assert.Equal("household.forbidden", result.Code());
        }
    }

    [Fact]
    public void D3a_AnInviteGivesAtMostMember()
    {
        var h = new AHousehold(_time);

        Assert.Equal("invite.role_invalid",
            h.Household.CreateInvite(h.Owner, HouseholdRole.Admin, InviteValidity.OneDay, null, _time).Code());
        Assert.Equal("invite.validity_invalid",
            h.Household.CreateInvite(h.Owner, HouseholdRole.Member, (InviteValidity)42, null, _time).Code());
    }

    [Fact]
    public void D2c_GuestAccessEndOnlyOnGuestInvitesAndInTheFuture()
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.access_until_invalid",
            h.Household.CreateInvite(h.Owner, HouseholdRole.Member, InviteValidity.OneDay, _time.Now.AddDays(3), _time).Code());
        Assert.Equal("household.access_until_invalid",
            h.Household.CreateInvite(h.Owner, HouseholdRole.Guest, InviteValidity.OneDay, _time.Now, _time).Code());
    }

    [Fact]
    public void I5_AcceptingJoinsWithTheInvitesRoleInviterAndAccessEnd()
    {
        var h = new AHousehold(_time);
        var until = _time.Now.AddDays(3);
        var invite = h.Household.CreateInvite(h.Member, HouseholdRole.Guest, InviteValidity.OneDay, until, _time).Value;
        var newcomer = Guid.NewGuid();

        Assert.True(h.Household.Accept(invite, newcomer, A.MaxMembers, _time).IsSuccess);

        var membership = h.Household.MemberOf(newcomer)!;
        Assert.Equal((HouseholdRole.Guest, h.Member.Id, until), (membership.Role, membership.InvitedBy, membership.AccessUntil));
        Assert.Equal(1, invite.Uses);
        var joined = Assert.IsType<MemberJoined>(Assert.Single(h.Household.DomainEvents));
        Assert.Equal((newcomer, HouseholdRole.Guest, (Guid?)h.Member.Id), (joined.UserId, joined.Role, joined.InvitedBy));
    }

    [Fact]
    public void D3b_InviteHasNoUseLimit()
    {
        var h = new AHousehold(_time);
        var invite = h.Household.CreateInvite(h.Owner, HouseholdRole.Member, InviteValidity.OneDay, null, _time).Value;

        for (var i = 0; i < 5; i++)
        {
            Assert.True(h.Household.Accept(invite, Guid.NewGuid(), A.MaxMembers, _time).IsSuccess);
        }

        Assert.Equal(5, invite.Uses);
    }

    [Fact]
    public void I5_AlreadyAMemberDoesNotCountAUse()
    {
        var h = new AHousehold(_time);
        var invite = h.Household.CreateInvite(h.Owner, HouseholdRole.Member, InviteValidity.OneDay, null, _time).Value;

        Assert.Equal("household.already_member", h.Household.Accept(invite, h.Member.Id, A.MaxMembers, _time).Code());
        Assert.Equal(0, invite.Uses);
    }

    /// <summary>Diagram 5.2 and the old <c>||</c> bug: usable only when not revoked AND not expired.</summary>
    [Fact]
    public void Lifecycle_ExpiredOrRevokedInviteIsNotUsable()
    {
        var h = new AHousehold(_time);
        var expiring = h.Household.CreateInvite(h.Owner, HouseholdRole.Member, InviteValidity.ThirtyMinutes, null, _time).Value;
        var revoked = h.Household.CreateInvite(h.Owner, HouseholdRole.Member, InviteValidity.SevenDays, null, _time).Value;
        Assert.True(h.Household.RevokeInvite(h.Owner, revoked, _time).IsSuccess);

        Assert.Equal("invite.not_usable", h.Household.Accept(revoked, Guid.NewGuid(), A.MaxMembers, _time).Code());
        _time.Advance(TimeSpan.FromMinutes(30));
        Assert.False(expiring.IsUsable(_time.Now));
        Assert.Equal("invite.not_usable", h.Household.Accept(expiring, Guid.NewGuid(), A.MaxMembers, _time).Code());
    }

    [Fact]
    public void I5_InviteOfAnotherHouseholdIsNotUsableHere()
    {
        var h = new AHousehold(_time);
        var other = new AHousehold(_time);
        var invite = other.Household.CreateInvite(other.Owner, HouseholdRole.Member, InviteValidity.OneDay, null, _time).Value;

        Assert.Equal("invite.not_usable", h.Household.Accept(invite, Guid.NewGuid(), A.MaxMembers, _time).Code());
        Assert.Equal("invite.not_found", h.Household.RevokeInvite(h.Owner, invite, _time).Code());
    }

    [Fact]
    public void D7_ArchivedHouseholdTakesNobody()
    {
        var h = new AHousehold(_time);
        var invite = h.Household.CreateInvite(h.Owner, HouseholdRole.Member, InviteValidity.OneDay, null, _time).Value;
        h.Household.Archive(h.Owner, _time);

        Assert.Equal("household.not_found", h.Household.Accept(invite, Guid.NewGuid(), A.MaxMembers, _time).Code());
    }

    [Fact]
    public void D3a_MemberRevokesAndSeesOnlyOwnInvitesAdminsAll()
    {
        var h = new AHousehold(_time);
        var ownersInvite = h.Household.CreateInvite(h.Owner, HouseholdRole.Member, InviteValidity.OneDay, null, _time).Value;
        var membersInvite = h.Household.CreateInvite(h.Member, HouseholdRole.Member, InviteValidity.OneDay, null, _time).Value;

        Assert.Equal("household.forbidden", h.Household.RevokeInvite(h.Member, ownersInvite, _time).Code());
        Assert.True(h.Household.RevokeInvite(h.Member, membersInvite, _time).IsSuccess);
        Assert.True(h.Household.RevokeInvite(h.Admin, ownersInvite, _time).IsSuccess);

        Assert.Equal(h.Member.Id, h.Household.InvitesVisibleTo(h.Member, _time).Value);
        Assert.Null(h.Household.InvitesVisibleTo(h.Admin, _time).Value);
        Assert.Equal("household.forbidden", h.Household.InvitesVisibleTo(h.Guest, _time).Code());
    }

    [Fact]
    public void RevokingTwiceKeepsTheFirstTime()
    {
        var h = new AHousehold(_time);
        var invite = h.Household.CreateInvite(h.Owner, HouseholdRole.Member, InviteValidity.OneDay, null, _time).Value;
        invite.Revoke(_time.Now);
        var first = invite.RevokedAt;

        _time.Advance(TimeSpan.FromMinutes(5));
        invite.Revoke(_time.Now);

        Assert.Equal(first, invite.RevokedAt);
    }

    [Fact]
    public void Q6_InviteOfAnErasedAuthorStaysUsable()
    {
        var h = new AHousehold(_time);
        var invite = h.Household.CreateInvite(h.Member, HouseholdRole.Member, InviteValidity.OneDay, null, _time).Value;

        invite.ForgetCreator();

        Assert.Null(invite.CreatedBy);
        Assert.True(h.Household.Accept(invite, Guid.NewGuid(), A.MaxMembers, _time).IsSuccess);
    }

    [Fact]
    public void D2b_GuestLinkLetsPeopleJoinAsGuestsUntilRegeneratedOrDisabled()
    {
        var h = new AHousehold(_time);
        Assert.Null(h.Household.GuestCode);
        Assert.Equal("household.forbidden", h.Household.RegenerateGuestLink(h.Member, _time).Code());

        var first = h.Household.RegenerateGuestLink(h.Admin, _time).Value;
        var visitor = Guid.NewGuid();
        Assert.True(h.Household.JoinAsGuest(visitor, first, A.MaxMembers, _time).IsSuccess);
        Assert.Equal(HouseholdRole.Guest, h.Household.MemberOf(visitor)!.Role);
        Assert.Null(h.Household.MemberOf(visitor)!.InvitedBy);

        var second = h.Household.RegenerateGuestLink(h.Owner, _time).Value;
        Assert.NotEqual(first, second);
        Assert.Equal("invite.not_found", h.Household.JoinAsGuest(Guid.NewGuid(), first, A.MaxMembers, _time).Code());

        Assert.True(h.Household.DisableGuestLink(h.Owner, _time).IsSuccess);
        Assert.Equal("invite.not_found", h.Household.JoinAsGuest(Guid.NewGuid(), second, A.MaxMembers, _time).Code());
    }
}
