using Households.Domain.Households;
using Households.Domain.Households.Events;

namespace Households.Domain.Tests;

public sealed class OwnershipTests
{
    private readonly FixedTime _time = new(A.Now);

    private static bool Everyone(Guid _) => true;

    [Fact]
    public void M5_OwnerHandsOverAndBecomesAdmin()
    {
        var h = new AHousehold(_time);

        Assert.True(h.Household.TransferOwnership(h.Owner, h.Member.Id, _time).IsSuccess);

        Assert.Equal(HouseholdRole.Owner, h.RoleOf(h.Member));
        Assert.Equal(HouseholdRole.Admin, h.RoleOf(h.Owner));
        Assert.Null(h.Household.OwnershipReceivedAt);
        var transferred = Assert.IsType<OwnershipTransferred>(Assert.Single(h.Household.DomainEvents));
        Assert.Equal((h.Owner.Id, h.Member.Id, OwnershipTransferReason.HandedOver),
            (transferred.FromUserId, transferred.ToUserId, transferred.Reason));
    }

    [Fact]
    public void M5_NotToAGuestNotToYourselfAndOnlyByTheOwner()
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.transfer_target_invalid", h.Household.TransferOwnership(h.Owner, h.Guest.Id, _time).Code());
        Assert.Equal("household.transfer_target_invalid", h.Household.TransferOwnership(h.Owner, h.Owner.Id, _time).Code());
        Assert.Equal("household.forbidden", h.Household.TransferOwnership(h.Admin, h.Admin.Id, _time).Code());
        Assert.Equal("household.not_member", h.Household.TransferOwnership(h.Owner, Guid.NewGuid(), _time).Code());
    }

    [Fact]
    public void D5_BanHandsOverToTheOldestActiveAdminAndTheOwnerDropsToMember()
    {
        var h = new AHousehold(_time);
        var youngerAdmin = h.Add(Guid.NewGuid(), HouseholdRole.Admin);
        h.Household.ClearDomainEvents();

        h.Household.OwnerBanned(h.Owner.Id, Everyone, _time);

        Assert.Equal(HouseholdRole.Owner, h.RoleOf(h.Admin));
        Assert.Equal(HouseholdRole.Admin, h.Household.MemberOf(youngerAdmin)!.Role);
        Assert.Equal(HouseholdRole.Member, h.RoleOf(h.Owner));
        Assert.Equal(_time.Now, h.Household.OwnershipReceivedAt);
        var transferred = Assert.IsType<OwnershipTransferred>(Assert.Single(h.Household.DomainEvents));
        Assert.Equal(OwnershipTransferReason.OwnerBanned, transferred.Reason);
    }

    [Fact]
    public void D5_D6_InactiveAdminIsSkippedForTheOldestActiveMember()
    {
        var h = new AHousehold(_time);

        h.Household.OwnerBanned(h.Owner.Id, id => id != h.Admin.Id, _time);

        Assert.Equal(HouseholdRole.Owner, h.RoleOf(h.Member));
        Assert.Equal(HouseholdRole.Admin, h.RoleOf(h.Admin));
    }

    [Fact]
    public void D5_BanWithNobodyToTakeOverChangesNothing()
    {
        var h = new AHousehold(_time);

        h.Household.OwnerBanned(h.Owner.Id, id => id == h.Owner.Id || id == h.Guest.Id, _time);

        Assert.Equal(HouseholdRole.Owner, h.RoleOf(h.Owner));
        Assert.Empty(h.Household.DomainEvents);
    }

    [Fact]
    public void D5_BanOfSomeoneElseThanTheOwnerChangesNothing()
    {
        var h = new AHousehold(_time);

        h.Household.OwnerBanned(h.Admin.Id, Everyone, _time);

        Assert.Equal(HouseholdRole.Admin, h.RoleOf(h.Admin));
        Assert.Empty(h.Household.DomainEvents);
    }

    [Fact]
    public void D5a_NewOwnerAcknowledgesTheBanner()
    {
        var h = new AHousehold(_time);
        h.Household.OwnerBanned(h.Owner.Id, Everyone, _time);

        Assert.Equal("household.forbidden", h.Household.AcknowledgeOwnership(h.Owner, _time).Code());
        Assert.True(h.Household.AcknowledgeOwnership(h.Admin, _time).IsSuccess);

        Assert.Null(h.Household.OwnershipReceivedAt);
    }

    [Fact]
    public void D5_ErasedOwnerLeavesAndTheOldestAdminTakesOver()
    {
        var h = new AHousehold(_time);

        h.Household.ForgetUser(h.Owner.Id, Everyone, _time);

        Assert.Null(h.Household.MemberOf(h.Owner.Id));
        Assert.Equal(HouseholdRole.Owner, h.RoleOf(h.Admin));
        Assert.Equal(OwnershipTransferReason.OwnerErased, Assert.IsType<OwnershipTransferred>(h.Household.DomainEvents[0]).Reason);
        Assert.Equal(MemberLeftReason.Erased, Assert.IsType<MemberLeft>(h.Household.DomainEvents[1]).Reason);
    }

    [Fact]
    public void D5_ErasedOwnerWithOnlyAGuestLeftArchivesTheHousehold()
    {
        var time = _time;
        var ownerId = Guid.NewGuid();
        var household = Household.Create(Guid.NewGuid(), ownerId, "Dom", null, time).Value;
        var invite = household.CreateInvite(A.User(ownerId), HouseholdRole.Guest, Invites.InviteValidity.OneDay, null, time).Value;
        household.Accept(invite, Guid.NewGuid(), A.MaxMembers, time);
        household.ClearDomainEvents();

        household.ForgetUser(ownerId, Everyone, time);

        Assert.Equal(HouseholdStatus.Archived, household.Status);
        Assert.Null(household.Owner);
        Assert.IsType<HouseholdArchived>(household.DomainEvents[0]);
        Assert.Equal("household.no_owner", household.Restore(A.ServerAdmin(), time).Code());
    }

    [Fact]
    public void Q6_ErasedMemberLeavesAndIsForgottenAsInviter()
    {
        var h = new AHousehold(_time);
        var invite = h.Household.CreateInvite(h.Member, HouseholdRole.Member, Invites.InviteValidity.OneDay, null, _time).Value;
        var friend = Guid.NewGuid();
        h.Household.Accept(invite, friend, A.MaxMembers, _time);
        Assert.Equal(h.Member.Id, h.Household.MemberOf(friend)!.InvitedBy);

        h.Household.ForgetUser(h.Member.Id, Everyone, _time);

        Assert.Null(h.Household.MemberOf(h.Member.Id));
        Assert.Null(h.Household.MemberOf(friend)!.InvitedBy);
        Assert.Equal(HouseholdRole.Owner, h.RoleOf(h.Owner));
    }
}
