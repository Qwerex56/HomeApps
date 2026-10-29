using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Accounts.Domain.Users.Events;

namespace Accounts.Domain.Tests;

public sealed class StatusTests
{
    private readonly FixedTime _time = new(A.Now);

    [Fact]
    public void UC03_UserDisablesOwnAccount()
    {
        var user = AUser.Registered(_time);

        Assert.True(user.Disable(user.Self(), "  przerwa  ", _time).IsSuccess);

        Assert.Equal(AccountStatus.Disabled, user.Status);
        Assert.True(user.IsDisabledBySelf);
        Assert.Equal("przerwa", user.Disabling!.Reason);
        Assert.Equal(A.Now, user.Disabling.At);
        Assert.True(Assert.IsType<UserDisabled>(Assert.Single(user.DomainEvents)).BySelf);
        Assert.Equal(HistoryAction.Disabled, user.History[^1].Action);
        Assert.Equal("przerwa", user.History[^1].Reason);
    }

    [Fact]
    public void UC14_AdminDisablesAUser()
    {
        var user = AUser.Registered(_time);
        var admin = AUser.Someone(ServerRole.Admin);

        Assert.True(user.Disable(admin, null, _time).IsSuccess);

        Assert.False(user.IsDisabledBySelf);
        Assert.Equal(admin.Id, user.Disabling!.ByUserId);
        Assert.False(Assert.IsType<UserDisabled>(Assert.Single(user.DomainEvents)).BySelf);
    }

    [Fact]
    public void R1_AdminCannotDisableAnotherAdminButOwnerCan()
    {
        var admin = AUser.Registered(_time, ServerRole.Admin);

        Assert.Equal("forbidden.role", admin.Disable(AUser.Someone(ServerRole.Admin), null, _time).Code());
        Assert.True(admin.Disable(AUser.Someone(ServerRole.Owner), null, _time).IsSuccess);
    }

    [Fact]
    public void R3_OwnerCannotBeDisabledNotEvenByThemselves()
    {
        var owner = AUser.Registered(_time, ServerRole.Owner);

        Assert.Equal("account.owner_protected", owner.Disable(owner.Self(), null, _time).Code());
        Assert.Equal(AccountStatus.Active, owner.Status);
    }

    [Fact]
    public void DisablingTwiceIsAConflict()
    {
        var user = AUser.Registered(_time);
        user.Disable(user.Self(), null, _time);

        Assert.Equal("account.already_disabled", user.Disable(AUser.Someone(ServerRole.Admin), null, _time).Code());
    }

    [Fact]
    public void ReasonIsLimited()
    {
        var user = AUser.Registered(_time);

        Assert.Equal("account.reason_invalid", user.Disable(user.Self(), new string('x', 501), _time).Code());
        Assert.Equal("account.reason_invalid", user.Disable(user.Self(), "a\u0007b", _time).Code());
        Assert.True(user.Disable(user.Self(), "linia 1\r\nlinia 2", _time).IsSuccess);
    }

    [Fact]
    public void UC14_HigherRoleEnablesAnyDisabledAccount()
    {
        var user = AUser.Registered(_time);
        user.Disable(user.Self(), null, _time);
        user.ClearDomainEvents();
        var admin = AUser.Someone(ServerRole.Admin);

        Assert.True(user.Enable(admin, _time).IsSuccess);

        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Null(user.Disabling);
        Assert.IsType<UserEnabled>(Assert.Single(user.DomainEvents));
        Assert.Equal(HistoryAction.Enabled, user.History[^1].Action);
    }

    [Fact]
    public void UsersDoNotEnableThemselvesOrPeersAndActiveAccountsAreNotEnabled()
    {
        var user = AUser.Registered(_time);
        Assert.Equal("account.not_disabled", user.Enable(AUser.Someone(ServerRole.Admin), _time).Code());

        user.Disable(AUser.Someone(ServerRole.Admin), null, _time);
        Assert.Equal("forbidden.role", user.Enable(user.Self(), _time).Code());
        Assert.Equal("forbidden.role", user.Enable(AUser.Someone(ServerRole.User), _time).Code());
    }

    [Fact]
    public void R8_SelfDisabledAccountIsRestoredWithCheckedProfile()
    {
        var user = AUser.Registered(_time);
        user.Disable(user.Self(), null, _time);
        user.ClearDomainEvents();

        Assert.True(user.ReactivateSelf(A.Profile("Adam N."), _time).IsSuccess);

        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Equal("Adam N.", user.Profile.DisplayName.Value);
        Assert.Collection(user.DomainEvents,
            e => Assert.IsType<UserProfileUpdated>(e),
            e => Assert.IsType<UserEnabled>(e));
        Assert.Equal(
            [HistoryAction.ProfileUpdated, HistoryAction.ReactivatedBySelf],
            user.History.TakeLast(2).Select(h => h.Action));
    }

    [Fact]
    public void R8_UncheckedOrUnchangedProfileOnlyEnables()
    {
        var user = AUser.Registered(_time);
        user.Disable(user.Self(), null, _time);
        user.ClearDomainEvents();

        Assert.True(user.ReactivateSelf(A.Profile(), _time).IsSuccess);
        Assert.IsType<UserEnabled>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void R8_AccountDisabledByAdminIsNotRestoredBySigningIn()
    {
        var user = AUser.Registered(_time);
        user.Disable(AUser.Someone(ServerRole.Admin), null, _time);

        Assert.Equal("reactivation.not_allowed", user.ReactivateSelf(null, _time).Code());
        Assert.Equal(AccountStatus.Disabled, user.Status);
    }

    [Fact]
    public void R8_ActiveAccountHasNothingToRestore() =>
        Assert.Equal("account.not_disabled", AUser.Registered(_time).ReactivateSelf(null, _time).Code());

    [Fact]
    public void DisabledAccountIsEditedOnlyByAHigherRole()
    {
        var user = AUser.Registered(_time);
        user.Disable(user.Self(), null, _time);

        Assert.Equal("account.disabled", user.UpdateProfile(user.Self(), A.Profile("Nowy"), _time).Code());
        Assert.True(user.UpdateProfile(AUser.Someone(ServerRole.Admin), A.Profile("Nowy"), _time).IsSuccess);
    }
}
