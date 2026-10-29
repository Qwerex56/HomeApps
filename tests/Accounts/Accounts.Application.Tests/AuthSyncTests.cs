using Accounts.Application.AuthSync;
using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Contracts = HomeApps.Contracts.Auth;

namespace Accounts.Application.Tests;

public sealed class AuthSyncTests
{
    private readonly World _w = new();

    private RegisterUserHandler Register => new(_w.Users, _w.Time);

    [Fact]
    public async Task UC08_IdentityRegisteredCreatesTheUser()
    {
        var identity = World.Identity() with
        {
            Source = Contracts.RegistrationSource.Google,
            ViaReferral = true,
            InvitedByEmail = "mama@example.com",
            InvitedEmail = "adam.invited@example.com",
        };

        Assert.True((await Register.Handle(new RegisterUserCommand(identity), default)).IsSuccess);

        var user = Assert.Single(_w.Users.Items);
        Assert.Equal(identity.UserId, user.Id);
        Assert.Equal("adam@example.com", user.Email.Value);
        Assert.Equal(RegistrationSource.Google, user.Registration.Source);
        Assert.Equal("adam.invited@example.com", user.Registration.InvitedEmail);
        Assert.Equal("pl-PL", user.Profile.Locale.Value);
    }

    [Fact]
    public async Task UC09_BootstrapIdentityBecomesOwner()
    {
        var identity = World.Identity(role: Contracts.ServerRole.Owner) with { Source = Contracts.RegistrationSource.Bootstrap };

        await Register.Handle(new RegisterUserCommand(identity), default);

        Assert.Equal(ServerRole.Owner, Assert.Single(_w.Users.Items).Role);
    }

    [Fact]
    public async Task R7_SecondDeliveryChangesNothing()
    {
        var identity = World.Identity();
        await Register.Handle(new RegisterUserCommand(identity), default);

        Assert.True((await Register.Handle(new RegisterUserCommand(identity with { DisplayName = "Other" }), default)).IsSuccess);

        Assert.Equal("Adam", Assert.Single(_w.Users.Items).Profile.DisplayName.Value);
    }

    [Fact]
    public async Task R7_ErasedIdIsNotBroughtBack()
    {
        var user = await _w.Registered();
        var owner = new Actor(Guid.NewGuid(), ServerRole.Owner);
        user.Disable(owner, null, _w.Time);
        _w.Users.AddErased(user.Erase(owner, "adam@example.com", _w.Time).Value);
        _w.Users.Remove(user);

        await Register.Handle(new RegisterUserCommand(World.Identity(user.Id)), default);

        Assert.Empty(_w.Users.Items);
    }

    [Fact]
    public async Task UC07_EmailAndRoleAreCopiedAndSignInMovesLastSeen()
    {
        var user = await _w.Registered();

        Assert.True((await new SyncEmailHandler(_w.Users, _w.Time).Handle(new SyncEmailCommand(user.Id, "Adam.Nowak@example.com"), default)).IsSuccess);
        Assert.True((await new SyncRoleHandler(_w.Users, _w.Time).Handle(new SyncRoleCommand(user.Id, Contracts.ServerRole.Admin), default)).IsSuccess);
        Assert.True((await new RecordSignInHandler(_w.Users).Handle(new RecordSignInCommand(user.Id, World.Start.AddHours(1)), default)).IsSuccess);

        Assert.Equal("adam.nowak@example.com", user.Email.Value);
        Assert.Equal(ServerRole.Admin, user.Role);
        Assert.Equal(World.Start.AddHours(1), user.LastSeenAt);
    }

    [Fact]
    public async Task EventBeforeRegistrationIsPendingSoItIsRetried()
    {
        var result = await new SyncRoleHandler(_w.Users, _w.Time).Handle(new SyncRoleCommand(Guid.NewGuid(), Contracts.ServerRole.Admin), default);

        Assert.Equal("user.pending", result.Code());
    }

    [Fact]
    public async Task EventForAnErasedUserIsIgnored()
    {
        var id = Guid.NewGuid();
        _w.Users.Erased.Add(TestErased.Of(id));

        Assert.True((await new RecordSignInHandler(_w.Users).Handle(new RecordSignInCommand(id, World.Start), default)).IsSuccess);
    }
}

internal static class TestErased
{
    /// <summary>An erased trace for <paramref name="id"/>, made the only way the domain allows: erasing a disabled user.</summary>
    public static ErasedUser Of(Guid id)
    {
        var time = new FixedTime(World.Start);
        var owner = new Actor(Guid.NewGuid(), ServerRole.Owner);
        var user = User.Register(id, Email.Create("gone@example.com").Value, ServerRole.User,
            Profile.FromRegistration("Gone", null, null, null, null),
            Domain.Users.Registration.Create(RegistrationSource.Password, World.Start, false, null, null, null, null, null), time);
        user.Disable(owner, null, time);
        return user.Erase(owner, "gone@example.com", time).Value;
    }
}
