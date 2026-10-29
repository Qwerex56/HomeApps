using Accounts.Application.MyAccount;
using Accounts.Application.Reactivation;
using Accounts.Domain.Common;
using Accounts.Domain.Users;

namespace Accounts.Application.Tests;

public sealed class MyAccountTests
{
    private readonly World _w = new();

    [Fact]
    public async Task UC01_ShowsMyAccountWithoutAdminData()
    {
        var user = await _w.Registered();
        _w.SignInAs(user);

        var view = (await new GetMyAccountHandler(_w.Users, _w.CurrentUser).Handle(new GetMyAccountQuery(), default)).Value;

        Assert.Equal(user.Id, view.Id);
        Assert.Equal("Adam", view.Profile.DisplayName);
        Assert.Equal("Europe/Warsaw", view.Profile.TimeZone);
        Assert.Equal(AccountStatus.Active, view.Status);
        Assert.Null(view.Disabling);
    }

    [Fact]
    public async Task UC01_AccountNotCreatedYetIsPending()
    {
        _w.SignInAsSomeone(ServerRole.User);

        var result = await new GetMyAccountHandler(_w.Users, _w.CurrentUser).Handle(new GetMyAccountQuery(), default);

        Assert.Equal("user.pending", result.Code());
    }

    [Fact]
    public async Task UC02_UpdatesTheWholeProfile()
    {
        var user = await _w.Registered();
        _w.SignInAs(user);

        var result = await new UpdateMyProfileHandler(_w.Users, _w.CurrentUser, _w.Time)
            .Handle(new UpdateMyProfileCommand(Forms.Profile("Adam N.", "Szefie")), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Szefie", user.Profile.PreferredName!.Value);
    }

    [Fact]
    public async Task UC02_InvalidFieldsComeBackTogether()
    {
        var user = await _w.Registered();
        _w.SignInAs(user);

        var result = await new UpdateMyProfileHandler(_w.Users, _w.CurrentUser, _w.Time)
            .Handle(new UpdateMyProfileCommand(Forms.Profile("A") with { TimeZone = "Mars/Base" }), default);

        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("Adam", user.Profile.DisplayName.Value);
    }

    [Fact]
    public async Task UC03_DisablesMyAccount()
    {
        var user = await _w.Registered();
        _w.SignInAs(user);

        await new DisableMyAccountHandler(_w.Users, _w.CurrentUser, _w.Time).Handle(new DisableMyAccountCommand("przerwa"), default);

        Assert.True(user.IsDisabledBySelf);
    }

    [Fact]
    public async Task UC04_Q6_MyHistoryShowsOnlyWhenAndWhatLimitedTo50()
    {
        var user = await _w.Registered();
        var admin = new Actor(Guid.NewGuid(), ServerRole.Admin);
        for (var i = 0; i < 60; i++)
        {
            _w.Time.Now = _w.Time.Now.AddMinutes(1);
            user.UpdateProfile(admin, Profile.Create($"Adam {i}", null, null, null, null, "pl-PL", "Europe/Warsaw", null, new DateOnly(2026, 10, 27)).Value, _w.Time);
        }

        _w.SignInAs(user);
        var history = (await new GetMyHistoryHandler(_w.Users, _w.CurrentUser).Handle(new GetMyHistoryQuery(), default)).Value;

        Assert.Equal(GetMyHistoryQuery.Limit, history.Count);
        Assert.Equal(_w.Time.Now, history[0].At);
        Assert.All(history, h => Assert.Equal(HistoryAction.ProfileUpdated, h.Action));
    }

    [Fact]
    public async Task UC05_SelfDisabledAccountShowsTheFormAndComesBack()
    {
        var user = await _w.Registered();
        _w.SignInAs(user);
        user.Disable(user.Self(), null, _w.Time);

        var form = await new GetReactivationFormHandler(_w.Users, _w.CurrentUser).Handle(new GetReactivationFormQuery(), default);
        var result = await new ReactivateMyAccountHandler(_w.Users, _w.CurrentUser, _w.Time)
            .Handle(new ReactivateMyAccountCommand(Forms.Profile("Adam N.")), default);

        Assert.Equal("Adam", form.Value.DisplayName);
        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Equal("Adam N.", user.Profile.DisplayName.Value);
    }

    [Fact]
    public async Task UC05_AccountDisabledByAdminHasNoForm()
    {
        var user = await _w.Registered();
        user.Disable(new Actor(Guid.NewGuid(), ServerRole.Admin), null, _w.Time);
        _w.SignInAs(user);

        var form = await new GetReactivationFormHandler(_w.Users, _w.CurrentUser).Handle(new GetReactivationFormQuery(), default);

        Assert.Equal("reactivation.not_allowed", form.Code());
    }
}

internal static class UserTestExtensions
{
    public static Actor Self(this User user) => new(user.Id, user.Role);
}
