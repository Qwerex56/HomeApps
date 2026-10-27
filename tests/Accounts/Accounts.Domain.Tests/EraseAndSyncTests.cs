using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Accounts.Domain.Users.Events;

namespace Accounts.Domain.Tests;

public sealed class EraseAndSyncTests
{
    private readonly FixedTime _time = new(A.Now);
    private readonly Actor _owner = AUser.Someone(ServerRole.Owner);

    private User Disabled(ServerRole role = ServerRole.User)
    {
        var user = AUser.Registered(_time, role);
        user.Disable(_owner, null, _time);
        user.ClearDomainEvents();
        return user;
    }

    [Fact]
    public void UC17_OwnerErasesADisabledAccountWithEmailConfirmation()
    {
        var user = Disabled();

        var erased = user.Erase(_owner, " ADAM@example.com ", _time);

        Assert.True(erased.IsSuccess);
        Assert.Equal(user.Id, erased.Value.Id);
        Assert.Equal(_owner.Id, erased.Value.ErasedBy);
        Assert.Equal(A.Now, erased.Value.ErasedAt);
        Assert.IsType<UserErased>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void R5_OnlyTheOwnerErases() =>
        Assert.Equal("forbidden.role", Disabled().Erase(AUser.Someone(ServerRole.Admin), "adam@example.com", _time).Code());

    [Fact]
    public void R5_ActiveAccountIsDisabledFirst() =>
        Assert.Equal("erase.not_disabled", AUser.Registered(_time).Erase(_owner, "adam@example.com", _time).Code());

    [Theory]
    [InlineData(null)]
    [InlineData("eve@example.com")]
    [InlineData("not an email")]
    public void Q1_WrongConfirmationStopsTheErase(string? confirm)
    {
        var user = Disabled();

        Assert.Equal("erase.confirmation_mismatch", user.Erase(_owner, confirm, _time).Code());
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void R3_OwnerIsNeverErased()
    {
        var owner = AUser.Registered(_time, ServerRole.Owner);

        Assert.Equal("account.owner_protected", owner.Erase(owner.Self(), "adam@example.com", _time).Code());
    }

    [Fact]
    public void SyncRoleCopiesTheRoleFromAuthOnce()
    {
        var user = AUser.Registered(_time);

        user.SyncRole(ServerRole.Admin, _time);
        user.SyncRole(ServerRole.Admin, _time);

        Assert.Equal(ServerRole.Admin, user.Role);
        Assert.Single(user.History, h => h.Action == HistoryAction.RoleSynced && h.Details == "Admin" && h.ActorId is null);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void UC07_SyncEmailCopiesTheEmailFromAuthOnce()
    {
        var user = AUser.Registered(_time);

        user.SyncEmail(A.Email("adam.nowak@example.com"), _time);
        user.SyncEmail(A.Email("adam.nowak@example.com"), _time);

        Assert.Equal("adam.nowak@example.com", user.Email.Value);
        Assert.Single(user.History, h => h.Action == HistoryAction.EmailSynced);
    }

    [Fact]
    public void LastSeenNeverMovesBack()
    {
        var user = AUser.Registered(_time);

        user.RecordSeen(A.Now.AddMinutes(30));
        user.RecordSeen(A.Now.AddMinutes(15));

        Assert.Equal(A.Now.AddMinutes(30), user.LastSeenAt);
        Assert.Single(user.History);
    }
}
