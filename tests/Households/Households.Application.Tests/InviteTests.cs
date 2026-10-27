using Households.Application.Households;
using Households.Application.Invites;
using Households.Application.Members;
using Households.Domain.Households;
using Households.Domain.Invites;
using Contracts = HomeApps.Contracts.Households;

namespace Households.Application.Tests;

public sealed class InviteTests
{
    [Fact]
    public async Task I1_I4_I5_InviteFromCreationToJoining()
    {
        using var w = new World();
        var f = await w.Family();
        w.SignInAs(f.Member);

        var invite = (await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Member, InviteValidity.OneDay, null))).Value;
        Assert.Equal(("Marek", w.Time.Now.AddDays(1)), (invite.CreatedByName, invite.ExpiresAt));

        var newcomer = await w.SignUp("Nowy");
        var preview = (await w.Send(new PreviewInviteQuery(invite.Code.ToLowerInvariant()))).Value;
        Assert.Equal(("Dom", HouseholdRole.Member, "Marek"), (preview.HouseholdName, preview.Role, preview.InvitedByName));

        Assert.Equal(f.Id, (await w.Send(new AcceptInviteCommand(invite.Code))).Value);
        Assert.Equal(f.Member, w.Household(f.Id).MemberOf(newcomer)!.InvitedBy);
        Assert.Equal(new Contracts.MemberJoined(f.Id, newcomer, Contracts.HouseholdRole.Member), Assert.Single(w.Bus.Published));
    }

    [Fact]
    public async Task I4_UnknownExpiredOrArchivedLinksAreNotFound()
    {
        using var w = new World();
        var f = await w.Family();
        var invite = (await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Member, InviteValidity.ThirtyMinutes, null))).Value;
        await w.SignUp("Nowy");

        Assert.Equal("invite.not_found", (await w.Send(new PreviewInviteQuery("nonsense"))).Code());
        Assert.Equal("invite.not_found", (await w.Send(new PreviewInviteQuery(InviteCode.Generate().Display))).Code());
        w.Time.Advance(TimeSpan.FromMinutes(30));
        Assert.Equal("invite.not_found", (await w.Send(new PreviewInviteQuery(invite.Code))).Code());
        Assert.Equal("invite.not_found", (await w.Send(new AcceptInviteCommand(invite.Code))).Code());
    }

    [Fact]
    public async Task D2b_GuestLinkPreviewsAndJoinsAsGuest()
    {
        using var w = new World();
        var f = await w.Family();
        var link = (await w.Send(new RegenerateGuestLinkCommand(f.Id))).Value;
        var visitor = await w.SignUp("Gość");

        var preview = (await w.Send(new PreviewInviteQuery(link))).Value;
        Assert.Equal((HouseholdRole.Guest, (string?)null), (preview.Role, preview.InvitedByName));
        Assert.True((await w.Send(new AcceptInviteCommand(link))).IsSuccess);
        Assert.Equal(HouseholdRole.Guest, w.Household(f.Id).MemberOf(visitor)!.Role);

        w.SignInAs(f.Owner);
        await w.Send(new DisableGuestLinkCommand(f.Id));
        await w.SignUp("Ktoś");
        Assert.Equal("invite.not_found", (await w.Send(new AcceptInviteCommand(link))).Code());
    }

    [Fact]
    public async Task Q5_DisabledAccountCannotJoin()
    {
        using var w = new World();
        var f = await w.Family();
        var invite = (await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Member, InviteValidity.OneDay, null))).Value;
        var me = await w.SignUp("Nowy");
        w.Users.Items.Single(u => u.Id == me).Disable();

        Assert.Equal("account.disabled", (await w.Send(new AcceptInviteCommand(invite.Code))).Code());
    }

    [Fact]
    public async Task D10_LimitComesFromSettings()
    {
        using var w = new World();
        var f = await w.Family();
        w.Settings.MaxMembers = 4;
        var invite = (await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Member, InviteValidity.OneDay, null))).Value;
        await w.SignUp("Nowy");

        Assert.Equal("household.full", (await w.Send(new AcceptInviteCommand(invite.Code))).Code());
    }

    [Fact]
    public async Task I2_I3_MemberSeesAndRevokesOwnInvitesAdminsAll()
    {
        using var w = new World();
        var f = await w.Family();
        var owners = (await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Guest, InviteValidity.OneDay, null))).Value;
        w.SignInAs(f.Member);
        var members = (await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Member, InviteValidity.OneDay, null))).Value;

        Assert.Equal([members.Id], (await w.Send(new ListInvitesQuery(f.Id))).Value.Select(i => i.Id));
        Assert.Equal("household.forbidden", (await w.Send(new RevokeInviteCommand(f.Id, owners.Id))).Code());
        Assert.Equal("invite.not_found", (await w.Send(new RevokeInviteCommand(f.Id, Guid.NewGuid()))).Code());

        w.SignInAs(f.Admin);
        var all = (await w.Send(new ListInvitesQuery(f.Id))).Value.Select(i => i.Id).ToList();
        Assert.Contains(owners.Id, all);
        Assert.Contains(members.Id, all);
        Assert.True((await w.Send(new RevokeInviteCommand(f.Id, members.Id))).IsSuccess);
        Assert.DoesNotContain(members.Id, (await w.Send(new ListInvitesQuery(f.Id))).Value.Select(i => i.Id));

        w.SignInAs(f.Guest);
        Assert.Equal("household.forbidden", (await w.Send(new ListInvitesQuery(f.Id))).Code());
        w.SignInAs(Guid.NewGuid());
        Assert.Equal("household.not_found", (await w.Send(new RevokeInviteCommand(f.Id, Guid.NewGuid()))).Code());
    }

    [Fact]
    public async Task D3c_LeavingOrBeingRemovedRevokesYourInvites()
    {
        using var w = new World();
        var f = await w.Family();
        w.SignInAs(f.Member);
        var leaving = (await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Member, InviteValidity.OneDay, null))).Value;
        w.SignInAs(f.Admin);
        var removed = (await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Member, InviteValidity.OneDay, null))).Value;
        w.SignInAs(f.Owner);
        var stays = (await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Member, InviteValidity.OneDay, null))).Value;

        w.SignInAs(f.Member);
        await w.Send(new LeaveHouseholdCommand(f.Id));
        w.SignInAs(f.Owner);
        await w.Send(new RemoveMemberCommand(f.Id, f.Admin));

        Assert.NotNull(w.Invites.Items.Single(i => i.Id == leaving.Id).RevokedAt);
        Assert.NotNull(w.Invites.Items.Single(i => i.Id == removed.Id).RevokedAt);
        Assert.Null(w.Invites.Items.Single(i => i.Id == stays.Id).RevokedAt);
    }

    [Fact]
    public async Task D7_ArchivingRevokesEveryInvite()
    {
        using var w = new World();
        var f = await w.Family();
        await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Member, InviteValidity.OneDay, null));

        await w.Send(new ArchiveHouseholdCommand(f.Id));

        Assert.All(w.Invites.Items, i => Assert.NotNull(i.RevokedAt));
    }
}
