using Households.Application.Admin;
using Households.Application.Households;
using Households.Domain.Common;
using Households.Domain.Households;
using Contracts = HomeApps.Contracts.Households;

namespace Households.Application.Tests;

public sealed class HouseholdTests
{
    [Fact]
    public async Task H1_CreatorBecomesOwnerAndTheEventIsPublished()
    {
        using var w = new World();
        var me = await w.SignUp();

        var id = (await w.Send(new CreateHouseholdCommand("Dom", "Nasz"))).Value;

        Assert.Equal(me, w.Household(id).Owner!.UserId);
        Assert.Equal(new Contracts.HouseholdCreated(id, me, "Dom"), Assert.Single(w.Bus.Published));
        Assert.Equal(2, w.Saves);
    }

    [Fact]
    public async Task Q5_DisabledAccountCreatesNothingButUnknownOneMay()
    {
        using var w = new World();
        var me = await w.SignUp();
        w.Users.Items.Single(u => u.Id == me).Disable();

        Assert.Equal("account.disabled", (await w.Send(new CreateHouseholdCommand("Dom", null))).Code());

        w.SignInAs(Guid.NewGuid());
        Assert.True((await w.Send(new CreateHouseholdCommand("Dom", null))).IsSuccess);
    }

    [Fact]
    public async Task H1_InvalidNameSavesNothing()
    {
        using var w = new World();
        await w.SignUp();
        var saves = w.Saves;

        Assert.Equal("household.name_invalid", (await w.Send(new CreateHouseholdCommand(" ", null))).Code());
        Assert.Equal(saves, w.Saves);
        Assert.Empty(w.Households.Items);
    }

    [Fact]
    public async Task H2_MyHouseholdsHideTheCountFromGuests()
    {
        using var w = new World();
        var f = await w.Family();

        var mine = (await w.Send(new GetMyHouseholdsQuery())).Value;
        Assert.Equal(4, Assert.Single(mine).MemberCount);

        w.SignInAs(f.Guest);
        var guests = Assert.Single((await w.Send(new GetMyHouseholdsQuery())).Value);
        Assert.Equal((HouseholdRole.Guest, (int?)null), (guests.MyRole, guests.MemberCount));
    }

    [Fact]
    public async Task H3_ViewDependsOnWhoAsks()
    {
        using var w = new World();
        var f = await w.Family();
        await w.Send(new RegenerateGuestLinkCommand(f.Id));

        var owner = (await w.Send(new GetHouseholdQuery(f.Id))).Value;
        Assert.Equal((HouseholdRole.Owner, (int?)4), (owner.MyRole, owner.MemberCount));
        Assert.Matches("^[0-9A-Z]{5}-[0-9A-Z]{5}$", owner.GuestLink!);

        w.SignInAs(f.Member);
        var member = (await w.Send(new GetHouseholdQuery(f.Id))).Value;
        Assert.Null(member.GuestLink);

        w.SignInAs(f.Guest);
        var guest = (await w.Send(new GetHouseholdQuery(f.Id))).Value;
        Assert.Equal(((int?)null, (string?)null), (guest.MemberCount, guest.GuestLink));

        w.SignInAs(Guid.NewGuid(), ServerRole.Admin);
        var serverAdmin = (await w.Send(new GetHouseholdQuery(f.Id))).Value;
        Assert.Equal(((HouseholdRole?)null, (int?)4), (serverAdmin.MyRole, serverAdmin.MemberCount));

        w.SignInAs(Guid.NewGuid());
        Assert.Equal("household.not_found", (await w.Send(new GetHouseholdQuery(f.Id))).Code());
        Assert.Equal("household.not_found", (await w.Send(new GetHouseholdQuery(Guid.NewGuid()))).Code());
    }

    [Fact]
    public async Task H4_RenamePublishesTheNewName()
    {
        using var w = new World();
        var f = await w.Family();
        w.SignInAs(f.Admin);

        Assert.True((await w.Send(new UpdateHouseholdCommand(f.Id, "Domek", null))).IsSuccess);

        Assert.Equal(new Contracts.HouseholdUpdated(f.Id, "Domek", null), Assert.Single(w.Bus.Published));
    }

    [Fact]
    public async Task D7_DeletedHouseholdIsGoneForMembersAndAServerAdminRestoresIt()
    {
        using var w = new World();
        var f = await w.Family();

        Assert.True((await w.Send(new ArchiveHouseholdCommand(f.Id))).IsSuccess);
        Assert.Empty((await w.Send(new GetMyHouseholdsQuery())).Value);
        Assert.Equal("household.not_found", (await w.Send(new RestoreHouseholdCommand(f.Id))).Code());

        w.SignInAs(Guid.NewGuid(), ServerRole.Owner);
        Assert.True((await w.Send(new RestoreHouseholdCommand(f.Id))).IsSuccess);
        Assert.Equal(
            [new Contracts.HouseholdArchived(f.Id), new Contracts.HouseholdRestored(f.Id)],
            w.Bus.Published);
    }

    [Fact]
    public async Task Q7_OnlyServerAdminsListEveryHouseholdNewestFirst()
    {
        using var w = new World();
        var first = await w.Family("Dom");
        w.Time.Advance(TimeSpan.FromDays(1));
        var second = await w.Family("Domek letni");

        Assert.Equal("forbidden.role", (await w.Send(new ListAllHouseholdsQuery(null, null))).Code());

        w.SignInAs(Guid.NewGuid(), ServerRole.Admin);
        var all = (await w.Send(new ListAllHouseholdsQuery(null, null))).Value;
        Assert.Equal([second.Id, first.Id], all.Select(h => h.Id));
        Assert.Equal("Ola", all[0].OwnerName);

        var found = (await w.Send(new ListAllHouseholdsQuery("LETNI", null))).Value;
        Assert.Equal(second.Id, Assert.Single(found).Id);

        var older = (await w.Send(new ListAllHouseholdsQuery(null, all[0].CreatedAt))).Value;
        Assert.Equal(first.Id, Assert.Single(older).Id);
    }

    [Fact]
    public async Task D5a_BannerIsShownToTheNewOwnerUntilAcknowledged()
    {
        using var w = new World();
        var f = await w.Family();
        await w.Send(new AccountsSync.DisableUserCopyCommand(f.Owner, BySelf: false));

        w.SignInAs(f.Admin);
        Assert.True(Assert.Single((await w.Send(new GetMyHouseholdsQuery())).Value).OwnershipReceived);
        Assert.True((await w.Send(new AcknowledgeOwnershipCommand(f.Id))).IsSuccess);
        Assert.False(Assert.Single((await w.Send(new GetMyHouseholdsQuery())).Value).OwnershipReceived);
    }
}
