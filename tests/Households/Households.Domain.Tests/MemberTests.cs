using Households.Domain.Households;
using Households.Domain.Households.Events;
using Households.Domain.Invites;

namespace Households.Domain.Tests;

public sealed class MemberTests
{
    private readonly FixedTime _time = new(A.Now);

    /// <summary>household.md 3 + D2a: who may remove whom.</summary>
    [Theory]
    [InlineData("owner", "admin", true)]
    [InlineData("owner", "member", true)]
    [InlineData("owner", "guest", true)]
    [InlineData("admin", "member", true)]
    [InlineData("admin", "guest", true)]
    [InlineData("admin", "owner", false)]
    [InlineData("member", "guest", false)]
    [InlineData("guest", "member", false)]
    public void M2_RemoveMatrix(string who, string target, bool allowed)
    {
        var h = new AHousehold(_time);

        var result = h.Household.RemoveMember(h.Of(who), h.Of(target).Id, _time);

        Assert.Equal(allowed, result.IsSuccess);
        if (allowed)
        {
            Assert.Null(h.Household.MemberOf(h.Of(target).Id));
            var left = Assert.IsType<MemberLeft>(Assert.Single(h.Household.DomainEvents));
            Assert.Equal(MemberLeftReason.Removed, left.Reason);
        }
        else
        {
            Assert.Equal("household.forbidden", result.Code());
        }
    }

    [Fact]
    public void M2_AdminCannotRemoveAnotherAdmin()
    {
        var h = new AHousehold(_time);
        var secondAdmin = A.User(h.Add(Guid.NewGuid(), HouseholdRole.Admin));

        Assert.Equal("household.forbidden", h.Household.RemoveMember(h.Admin, secondAdmin.Id, _time).Code());
    }

    [Fact]
    public void M2_RemovingSomeoneWhoIsNotThereIsNotMember()
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.not_member", h.Household.RemoveMember(h.Owner, Guid.NewGuid(), _time).Code());
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("member")]
    [InlineData("guest")]
    public void M3_AnyoneButTheOwnerLeaves(string who)
    {
        var h = new AHousehold(_time);

        Assert.True(h.Household.Leave(h.Of(who), _time).IsSuccess);

        Assert.Equal(MemberLeftReason.Left, Assert.IsType<MemberLeft>(Assert.Single(h.Household.DomainEvents)).Reason);
    }

    [Fact]
    public void D5_OwnerHandsOverBeforeLeaving()
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.owner_cannot_leave", h.Household.Leave(h.Owner, _time).Code());
    }

    [Fact]
    public void M4_OwnerPromotesAndDemotes()
    {
        var h = new AHousehold(_time);

        Assert.True(h.Household.ChangeRole(h.Owner, h.Member.Id, HouseholdRole.Admin, _time).IsSuccess);
        Assert.True(h.Household.ChangeRole(h.Owner, h.Guest.Id, HouseholdRole.Member, _time).IsSuccess);
        Assert.True(h.Household.ChangeRole(h.Owner, h.Admin.Id, HouseholdRole.Guest, _time).IsSuccess);

        Assert.Equal(HouseholdRole.Admin, h.RoleOf(h.Member));
        Assert.Equal(HouseholdRole.Member, h.RoleOf(h.Guest));
        Assert.Equal(HouseholdRole.Guest, h.RoleOf(h.Admin));
        var first = Assert.IsType<MemberRoleChanged>(h.Household.DomainEvents[0]);
        Assert.Equal((HouseholdRole.Member, HouseholdRole.Admin), (first.OldRole, first.NewRole));
    }

    [Fact]
    public void M4_OnlyTheOwnerChangesRolesAndNeverToOrFromOwner()
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.forbidden", h.Household.ChangeRole(h.Admin, h.Member.Id, HouseholdRole.Admin, _time).Code());
        Assert.Equal("household.role_invalid", h.Household.ChangeRole(h.Owner, h.Member.Id, HouseholdRole.Owner, _time).Code());
        Assert.Equal("household.forbidden", h.Household.ChangeRole(h.Owner, h.Owner.Id, HouseholdRole.Admin, _time).Code());
        Assert.Equal("household.role_unchanged", h.Household.ChangeRole(h.Owner, h.Member.Id, HouseholdRole.Member, _time).Code());
    }

    [Fact]
    public void M6_MembersSetTheirOwnNicknameAndBlankClearsIt()
    {
        var h = new AHousehold(_time);

        Assert.True(h.Household.SetNickname(h.Member, h.Member.Id, " Tata ", _time).IsSuccess);
        Assert.Equal("Tata", h.Household.MemberOf(h.Member.Id)!.Nickname!.Value);

        Assert.True(h.Household.SetNickname(h.Member, h.Member.Id, " ", _time).IsSuccess);
        Assert.Null(h.Household.MemberOf(h.Member.Id)!.Nickname);
    }

    [Fact]
    public void D2a_GuestIsNamedByOwnerOrAdminNotByThemselvesAndMembersNameOnlyThemselves()
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.forbidden", h.Household.SetNickname(h.Guest, h.Guest.Id, "Ja", _time).Code());
        Assert.True(h.Household.SetNickname(h.Admin, h.Guest.Id, "Ciocia", _time).IsSuccess);
        Assert.Equal("household.forbidden", h.Household.SetNickname(h.Admin, h.Member.Id, "X", _time).Code());
        Assert.Equal("household.forbidden", h.Household.SetNickname(h.Member, h.Guest.Id, "X", _time).Code());
    }

    [Fact]
    public void D10_HouseholdIsFullAtTheLimit()
    {
        var h = new AHousehold(_time);
        var invite = h.Household.CreateInvite(h.Owner, HouseholdRole.Member, InviteValidity.OneDay, null, _time).Value;

        var result = h.Household.Accept(invite, Guid.NewGuid(), maxMembers: h.Household.Members.Count, _time);

        Assert.Equal("household.full", result.Code());
        Assert.Equal(0, invite.Uses);
    }

    [Fact]
    public void D2c_GuestAccessEndsAndTheCleanUpRemovesThem()
    {
        var h = new AHousehold(_time);
        var until = _time.Now.AddDays(2);
        Assert.True(h.Household.SetGuestAccess(h.Admin, h.Guest.Id, until, _time).IsSuccess);

        _time.Now = until;

        Assert.False(h.Household.CanRead(h.Guest, _time.Now));
        Assert.Equal("household.not_found", h.Household.Leave(h.Guest, _time).Code());
        Assert.Equal(1, h.Household.RemoveExpiredGuests(_time));
        Assert.Null(h.Household.MemberOf(h.Guest.Id));
        Assert.Equal(MemberLeftReason.Expired, Assert.IsType<MemberLeft>(h.Household.DomainEvents[^1]).Reason);
    }

    [Fact]
    public void D2c_AccessEndIsOnlyForGuestsInTheFutureAndSetByOwnerOrAdmin()
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.not_guest", h.Household.SetGuestAccess(h.Owner, h.Member.Id, _time.Now.AddDays(1), _time).Code());
        Assert.Equal("household.access_until_invalid", h.Household.SetGuestAccess(h.Owner, h.Guest.Id, _time.Now, _time).Code());
        Assert.Equal("household.forbidden", h.Household.SetGuestAccess(h.Member, h.Guest.Id, _time.Now.AddDays(1), _time).Code());
        Assert.True(h.Household.SetGuestAccess(h.Owner, h.Guest.Id, null, _time).IsSuccess);
    }

    [Fact]
    public void D2c_PromotingAGuestDropsTheAccessEnd()
    {
        var h = new AHousehold(_time);
        h.Household.SetGuestAccess(h.Owner, h.Guest.Id, _time.Now.AddDays(1), _time);

        h.Household.ChangeRole(h.Owner, h.Guest.Id, HouseholdRole.Member, _time);

        Assert.Null(h.Household.MemberOf(h.Guest.Id)!.AccessUntil);
    }

    [Fact]
    public void D2c_AGuestWhoseAccessEndedMayJoinAgainBeforeTheCleanUp()
    {
        var h = new AHousehold(_time);
        h.Household.SetGuestAccess(h.Owner, h.Guest.Id, _time.Now.AddHours(1), _time);
        _time.Advance(TimeSpan.FromHours(2));
        var invite = h.Household.CreateInvite(h.Member, HouseholdRole.Guest, InviteValidity.OneDay, null, _time).Value;
        h.Household.ClearDomainEvents();

        Assert.True(h.Household.Accept(invite, h.Guest.Id, A.MaxMembers, _time).IsSuccess);

        Assert.True(h.Household.CanRead(h.Guest, _time.Now));
        Assert.IsType<MemberLeft>(h.Household.DomainEvents[0]);
        Assert.IsType<MemberJoined>(h.Household.DomainEvents[1]);
    }
}
