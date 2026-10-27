using Households.Application.Members;
using Households.Domain.Households;
using Contracts = HomeApps.Contracts.Households;

namespace Households.Application.Tests;

public sealed class MemberTests
{
    [Fact]
    public async Task M1_ListShowsNicknameOrNameRoleActivityAndInviter()
    {
        using var w = new World();
        var f = await w.Family();
        w.SignInAs(f.Member);
        await w.Send(new SetNicknameCommand(f.Id, f.Member, "Tata"));
        w.Users.Items.Single(u => u.Id == f.Admin).Disable();

        var list = (await w.Send(new ListMembersQuery(f.Id))).Value;

        Assert.Equal(["Ola", "Adam", "Tata", "Gosia"], list.Select(m => m.Name));
        Assert.Equal([HouseholdRole.Owner, HouseholdRole.Admin, HouseholdRole.Member, HouseholdRole.Guest], list.Select(m => m.Role));
        Assert.False(list[1].IsActive);
        Assert.Equal(("Marek", "Ola"), (list[2].DisplayName, list[2].InvitedByName));
    }

    [Fact]
    public async Task D2a_GuestMayNotSeeTheListAndAStrangerDoesNotKnowIt()
    {
        using var w = new World();
        var f = await w.Family();

        w.SignInAs(f.Guest);
        Assert.Equal("household.forbidden", (await w.Send(new ListMembersQuery(f.Id))).Code());
        w.SignInAs(Guid.NewGuid());
        Assert.Equal("household.not_found", (await w.Send(new ListMembersQuery(f.Id))).Code());
    }

    [Fact]
    public async Task S5_MyMembership()
    {
        using var w = new World();
        var f = await w.Family();
        w.SignInAs(f.Guest);

        var me = (await w.Send(new GetMyMembershipQuery(f.Id))).Value;

        Assert.Equal((f.Id, HouseholdRole.Guest), (me.HouseholdId, me.Role));
        w.SignInAs(Guid.NewGuid());
        Assert.Equal("household.not_found", (await w.Send(new GetMyMembershipQuery(f.Id))).Code());
    }

    [Fact]
    public async Task M2_M3_M4_M5_ArePublished()
    {
        using var w = new World();
        var f = await w.Family();

        await w.Send(new ChangeMemberRoleCommand(f.Id, f.Member, HouseholdRole.Admin));
        await w.Send(new RemoveMemberCommand(f.Id, f.Guest));
        await w.Send(new TransferOwnershipCommand(f.Id, f.Member));
        w.SignInAs(f.Admin);
        await w.Send(new LeaveHouseholdCommand(f.Id));

        Assert.Equal(
            [
                new Contracts.MemberRoleChanged(f.Id, f.Member, Contracts.HouseholdRole.Member, Contracts.HouseholdRole.Admin),
                new Contracts.MemberLeft(f.Id, f.Guest, Contracts.MemberLeftReason.Removed),
                new Contracts.OwnershipTransferred(f.Id, "Dom", f.Owner, f.Member, Contracts.OwnershipTransferReason.HandedOver),
                new Contracts.MemberLeft(f.Id, f.Admin, Contracts.MemberLeftReason.Left),
            ],
            w.Bus.Published);
    }

    [Fact]
    public async Task D2c_HourlyCleanUpRemovesExpiredGuests()
    {
        using var w = new World();
        var f = await w.Family();
        await w.Send(new SetGuestAccessCommand(f.Id, f.Guest, w.Time.Now.AddHours(2)));

        Assert.Equal(0, (await w.Send(new RemoveExpiredGuestsCommand())).Value);
        w.Time.Advance(TimeSpan.FromHours(2));
        Assert.Equal(1, (await w.Send(new RemoveExpiredGuestsCommand())).Value);

        Assert.Null(w.Household(f.Id).MemberOf(f.Guest));
        Assert.Equal(new Contracts.MemberLeft(f.Id, f.Guest, Contracts.MemberLeftReason.Expired), Assert.Single(w.Bus.Published));
    }

    [Fact]
    public async Task ValidatorRejectsAnUnknownRoleBeforeTheHandler()
    {
        using var w = new World();
        var f = await w.Family();
        var saves = w.Saves;

        var result = await w.Send(new ChangeMemberRoleCommand(f.Id, f.Member, (HouseholdRole)9));

        Assert.True(result.IsFailed);
        Assert.Equal(saves, w.Saves);
    }
}
