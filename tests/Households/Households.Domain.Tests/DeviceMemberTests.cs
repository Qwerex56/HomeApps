using Households.Domain.Households;
using Households.Domain.Households.Events;
using Households.Domain.Invites;

namespace Households.Domain.Tests;

/// <summary>jarvis-satellites.md 3a (S13): a household satellite is a Member that only Devices adds and removes.</summary>
public sealed class DeviceMemberTests
{
    private readonly FixedTime _time = new(A.Now);
    private readonly Guid _device = Guid.NewGuid();

    [Fact]
    public void A_paired_satellite_joins_as_a_Member_once()
    {
        var h = new AHousehold(_time);

        Assert.True(h.Household.AddDevice(_device, _time).IsSuccess);
        Assert.True(h.Household.AddDevice(_device, _time).IsSuccess);

        var membership = h.Household.MemberOf(_device)!;
        Assert.True(membership.IsDevice);
        Assert.Equal(HouseholdRole.Member, membership.Role);
        Assert.IsType<MemberJoined>(Assert.Single(h.Household.DomainEvents));
    }

    [Fact]
    public void Its_role_and_membership_are_not_changed_by_people_and_it_never_takes_over()
    {
        var h = new AHousehold(_time);
        h.Household.AddDevice(_device, _time);

        Assert.Equal("household.device_member", h.Household.ChangeRole(h.Owner, _device, HouseholdRole.Admin, _time).Code());
        Assert.Equal("household.device_member", h.Household.RemoveMember(h.Owner, _device, _time).Code());
        Assert.Equal("household.transfer_target_invalid", h.Household.TransferOwnership(h.Owner, _device, _time).Code());
    }

    [Fact]
    public void It_does_not_count_towards_the_member_limit()
    {
        var h = new AHousehold(_time);
        h.Household.AddDevice(_device, _time);

        var invite = h.Household.CreateInvite(h.Owner, HouseholdRole.Member, InviteValidity.SevenDays, null, _time).Value;

        // Four people and the satellite: a fifth person still fits a limit of five.
        Assert.True(h.Household.Accept(invite, Guid.NewGuid(), 5, _time).IsSuccess);
    }

    [Fact]
    public void Unpairing_removes_it_and_a_second_time_changes_nothing()
    {
        var h = new AHousehold(_time);
        h.Household.AddDevice(_device, _time);
        h.Household.ClearDomainEvents();

        h.Household.RemoveDevice(_device, _time);
        h.Household.RemoveDevice(_device, _time);
        h.Household.RemoveDevice(h.Member.Id, _time);

        Assert.Null(h.Household.MemberOf(_device));
        Assert.NotNull(h.Household.MemberOf(h.Member.Id));
        Assert.Equal(MemberLeftReason.Removed, Assert.IsType<MemberLeft>(Assert.Single(h.Household.DomainEvents)).Reason);
    }
}
