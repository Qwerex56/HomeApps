using Accounts.Application.Abstractions;
using Accounts.Application.Admin;
using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Contracts = HomeApps.Contracts.Auth;

namespace Accounts.Application.Tests;

public sealed class AdminTests
{
    private readonly World _w = new();

    private async Task<User> SignedInAdmin(Contracts.ServerRole role = Contracts.ServerRole.Admin)
    {
        var admin = await _w.Registered("admin@example.com", "Ala Admin", role);
        _w.SignInAs(admin);
        return admin;
    }

    [Fact]
    public async Task UC10_ListFiltersSearchesAndPages()
    {
        await _w.Registered("adam@example.com", "Adam");
        await _w.Registered("ewa@example.com", "Ewa");
        await _w.Registered("zenon@example.com", "Zenon");
        await SignedInAdmin();

        var page = (await new ListUsersHandler(_w.Users, _w.CurrentUser)
            .Handle(new ListUsersQuery(Role: ServerRole.User, PageSize: 2), default)).Value;
        var search = (await new ListUsersHandler(_w.Users, _w.CurrentUser)
            .Handle(new ListUsersQuery(Search: "  EWA "), default)).Value;

        Assert.Equal(3, page.Total);
        Assert.Equal(["Adam", "Ewa"], page.Items.Select(i => i.DisplayName));
        Assert.Equal("ewa@example.com", Assert.Single(search.Items).Email);
    }

    [Fact]
    public async Task AdminScreensNeedAdmin()
    {
        var user = await _w.Registered();
        _w.SignInAs(user);

        Assert.Equal("forbidden.role", (await new ListUsersHandler(_w.Users, _w.CurrentUser).Handle(new ListUsersQuery(), default)).Code());
        Assert.Equal("forbidden.role", (await new GetUserHandler(_w.Users, _w.CurrentUser).Handle(new GetUserQuery(user.Id), default)).Code());
        Assert.Equal("forbidden.role", (await new DisableUserHandler(_w.Users, _w.CurrentUser, _w.Time)
            .Handle(new DisableUserCommand(user.Id, null), default)).Code());
    }

    [Fact]
    public async Task UC11_AdminSeesWhoDisabledTheAccount()
    {
        var user = await _w.Registered();
        var admin = await SignedInAdmin();
        await new DisableUserHandler(_w.Users, _w.CurrentUser, _w.Time).Handle(new DisableUserCommand(user.Id, "spam"), default);

        var view = (await new GetUserHandler(_w.Users, _w.CurrentUser).Handle(new GetUserQuery(user.Id), default)).Value;

        Assert.Equal(AccountStatus.Disabled, view.Status);
        Assert.Equal(admin.Id, view.Disabling!.ByUserId);
        Assert.False(view.Disabling.BySelf);
        Assert.Equal("spam", view.Disabling.Reason);
    }

    [Fact]
    public async Task UnknownAccountIsNotFound()
    {
        await SignedInAdmin();

        Assert.Equal("user.not_found", (await new GetUserHandler(_w.Users, _w.CurrentUser).Handle(new GetUserQuery(Guid.NewGuid()), default)).Code());
        Assert.Equal("user.not_found", (await new EnableUserHandler(_w.Users, _w.CurrentUser, _w.Time)
            .Handle(new EnableUserCommand(Guid.NewGuid()), default)).Code());
    }

    [Fact]
    public async Task UC13_UC14_AdminEditsDisablesAndEnablesAUser()
    {
        var user = await _w.Registered();
        await SignedInAdmin();

        Assert.True((await new UpdateUserProfileHandler(_w.Users, _w.CurrentUser, _w.Time)
            .Handle(new UpdateUserProfileCommand(user.Id, Forms.Profile("Adaś")), default)).IsSuccess);
        Assert.True((await new DisableUserHandler(_w.Users, _w.CurrentUser, _w.Time)
            .Handle(new DisableUserCommand(user.Id, null), default)).IsSuccess);
        Assert.True((await new EnableUserHandler(_w.Users, _w.CurrentUser, _w.Time)
            .Handle(new EnableUserCommand(user.Id), default)).IsSuccess);

        Assert.Equal("Adaś", user.Profile.DisplayName.Value);
        Assert.Equal(AccountStatus.Active, user.Status);
    }

    [Fact]
    public async Task R1_AdminCannotTouchAnotherAdmin()
    {
        var other = await _w.Registered("other@example.com", "Other", Contracts.ServerRole.Admin);
        await SignedInAdmin();

        var result = await new DisableUserHandler(_w.Users, _w.CurrentUser, _w.Time).Handle(new DisableUserCommand(other.Id, null), default);

        Assert.Equal("forbidden.role", result.Code());
    }

    [Fact]
    public async Task UC16_Q6_FullHistoryWithActorNamesAndCursor()
    {
        var user = await _w.Registered();
        var admin = await SignedInAdmin();
        for (var i = 0; i < 120; i++)
        {
            _w.Time.Now = _w.Time.Now.AddMinutes(1);
            await new UpdateUserProfileHandler(_w.Users, _w.CurrentUser, _w.Time)
                .Handle(new UpdateUserProfileCommand(user.Id, Forms.Profile($"Adam {i}")), default);
        }

        var handler = new GetUserHistoryHandler(_w.Users, _w.Users, _w.CurrentUser);
        var first = (await handler.Handle(new GetUserHistoryQuery(user.Id), default)).Value;
        var rest = (await handler.Handle(new GetUserHistoryQuery(user.Id, first[^1].At), default)).Value;

        Assert.Equal(GetUserHistoryQuery.Limit, first.Count);
        Assert.Equal("Ala Admin", first[0].ActorName);
        Assert.Equal(admin.Id, first[0].ActorId);
        Assert.Equal("DisplayName", first[0].Details);
        Assert.Equal(21, rest.Count);
        Assert.Equal(HistoryAction.Registered, rest[^1].Action);
        Assert.Null(rest[^1].ActorName);
    }

    [Fact]
    public async Task UC17_OwnerErasesADisabledAccountLeavingATrace()
    {
        var user = await _w.Registered();
        var owner = await SignedInAdmin(Contracts.ServerRole.Owner);
        await new DisableUserHandler(_w.Users, _w.CurrentUser, _w.Time).Handle(new DisableUserCommand(user.Id, null), default);
        user.ClearDomainEvents();

        var result = await new EraseUserHandler(_w.Users, _w.CurrentUser, _w.Time)
            .Handle(new EraseUserCommand(user.Id, "adam@example.com"), default);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(user, _w.Users.Items);
        var trace = Assert.Single(_w.Users.Erased);
        Assert.Equal(owner.Id, trace.ErasedBy);
        Assert.IsType<Domain.Users.Events.UserErased>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public async Task UC17_WrongConfirmationKeepsTheAccount()
    {
        var user = await _w.Registered();
        await SignedInAdmin(Contracts.ServerRole.Owner);
        await new DisableUserHandler(_w.Users, _w.CurrentUser, _w.Time).Handle(new DisableUserCommand(user.Id, null), default);

        var result = await new EraseUserHandler(_w.Users, _w.CurrentUser, _w.Time)
            .Handle(new EraseUserCommand(user.Id, "someone@example.com"), default);

        Assert.Equal("erase.confirmation_mismatch", result.Code());
        Assert.Contains(user, _w.Users.Items);
        Assert.Empty(_w.Users.Erased);
    }
}
