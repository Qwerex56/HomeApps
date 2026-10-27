using Households.Application.AccountsSync;
using Households.Application.Common;
using Households.Application.Invites;
using Households.Domain.Households;
using Households.Domain.Invites;
using Contracts = HomeApps.Contracts.Households;

namespace Households.Application.Tests;

public sealed class AccountsSyncTests
{
    [Fact]
    public async Task S1_SecondUserCreatedChangesNothing()
    {
        using var w = new World();
        var id = Guid.NewGuid();

        await w.Send(new CreateUserCopyCommand(id, "Ania", null));
        await w.Send(new CreateUserCopyCommand(id, "Inna", null));

        Assert.Equal("Ania", Assert.Single(w.Users.Items).DisplayName);
    }

    [Fact]
    public async Task S2_S3_EventsBeforeUserCreatedArePending()
    {
        using var w = new World();
        var id = Guid.NewGuid();

        Assert.Equal(ApplicationErrors.UserPendingCode, (await w.Send(new UpdateUserCopyCommand(id, "A", null))).Code());
        Assert.Equal(ApplicationErrors.UserPendingCode, (await w.Send(new DisableUserCopyCommand(id, true))).Code());
        Assert.Equal(ApplicationErrors.UserPendingCode, (await w.Send(new EnableUserCopyCommand(id))).Code());
    }

    [Fact]
    public async Task S2_S3_ProfileAndActivityAreCopied()
    {
        using var w = new World();
        var id = await w.SignUp("Ania");

        await w.Send(new UpdateUserCopyCommand(id, "Anna", "https://example.com/a.png"));
        await w.Send(new DisableUserCopyCommand(id, true));

        var copy = Assert.Single(w.Users.Items);
        Assert.Equal(("Anna", false), (copy.DisplayName, copy.IsActive));
        await w.Send(new EnableUserCopyCommand(id));
        Assert.True(copy.IsActive);
    }

    [Fact]
    public async Task D5b_SelfDisabledOwnerKeepsTheHousehold()
    {
        using var w = new World();
        var f = await w.Family();

        await w.Send(new DisableUserCopyCommand(f.Owner, BySelf: true));

        Assert.Equal(f.Owner, w.Household(f.Id).Owner!.UserId);
        Assert.Empty(w.Bus.Published);
    }

    [Fact]
    public async Task D5_BannedOwnerIsReplacedAndStaysAMemberWhenUnbanned()
    {
        using var w = new World();
        var f = await w.Family();

        await w.Send(new DisableUserCopyCommand(f.Owner, BySelf: false));
        await w.Send(new EnableUserCopyCommand(f.Owner));

        var household = w.Household(f.Id);
        Assert.Equal(f.Admin, household.Owner!.UserId);
        Assert.Equal(HouseholdRole.Member, household.MemberOf(f.Owner)!.Role);
        Assert.Equal(
            new Contracts.OwnershipTransferred(f.Id, "Dom", f.Owner, f.Admin, Contracts.OwnershipTransferReason.OwnerBanned),
            Assert.Single(w.Bus.Published));
    }

    [Fact]
    public async Task D5_D6_BanSkipsAnInactiveAdmin()
    {
        using var w = new World();
        var f = await w.Family();
        await w.Send(new DisableUserCopyCommand(f.Admin, BySelf: true));

        await w.Send(new DisableUserCopyCommand(f.Owner, BySelf: false));

        Assert.Equal(f.Member, w.Household(f.Id).Owner!.UserId);
    }

    [Fact]
    public async Task S4_ErasedOwnerIsForgottenEverywhereAndTheirInvitesKeepWorking()
    {
        using var w = new World();
        var f = await w.Family();
        var invite = (await w.Send(new CreateInviteCommand(f.Id, HouseholdRole.Member, InviteValidity.OneDay, null))).Value;

        Assert.True((await w.Send(new ForgetUserCommand(f.Owner))).IsSuccess);

        var household = w.Household(f.Id);
        Assert.Null(household.MemberOf(f.Owner));
        Assert.Equal(f.Admin, household.Owner!.UserId);
        Assert.Null(household.MemberOf(f.Admin)!.InvitedBy);
        Assert.DoesNotContain(w.Users.Items, u => u.Id == f.Owner);
        var kept = w.Invites.Items.Single(i => i.Id == invite.Id);
        Assert.Equal(((Guid?)null, (DateTimeOffset?)null), (kept.CreatedBy, kept.RevokedAt));
        Assert.Equal(
            [
                new Contracts.OwnershipTransferred(f.Id, "Dom", f.Owner, f.Admin, Contracts.OwnershipTransferReason.OwnerErased),
                new Contracts.MemberLeft(f.Id, f.Owner, Contracts.MemberLeftReason.Erased),
            ],
            w.Bus.Published);
    }

    [Fact]
    public async Task S4_ErasingAnUnknownUserDoesNothing()
    {
        using var w = new World();

        Assert.True((await w.Send(new ForgetUserCommand(Guid.NewGuid()))).IsSuccess);
    }
}
