using Accounts.Application.Abstractions;
using Accounts.Application.Admin.GetServerAbout;
using Accounts.Domain.Common;

namespace Accounts.Application.Tests;

/// <summary>Req 2.3 "About the server": Admin and Owner only; GitHub offline leaves the update question open.</summary>
public sealed class GetServerAboutTests
{
    private readonly World _w = new();

    private GetServerAboutHandler Handler(string? latestTag) => new(new FakeServerInfo(latestTag), _w.CurrentUser);

    [Fact]
    public async Task An_admin_sees_every_service_and_plugin_and_that_a_newer_release_is_out()
    {
        _w.SignInAsSomeone(ServerRole.Admin);

        var about = (await Handler("v999.0.0").Handle(new GetServerAboutQuery(), default)).Value;

        Assert.True(about.IsUpdateAvailable);
        Assert.Equal("999.0.0", about.LatestVersion);
        Assert.Equal("https://github.com/qwerex56/HomeApps/releases/tag/v999.0.0", about.ReleaseUrl);
        Assert.Equal(["Auth", "Households"], about.Services.Select(s => s.Name));
        Assert.Equal(["todo"], about.Plugins.Select(p => p.Id));
    }

    [Fact]
    public async Task An_owner_may_read_it()
    {
        _w.SignInAsSomeone(ServerRole.Owner);

        Assert.True((await Handler("v0.0.0").Handle(new GetServerAboutQuery(), default)).IsSuccess);
    }

    [Fact]
    public async Task A_release_not_newer_than_the_running_version_is_not_an_update()
    {
        _w.SignInAsSomeone(ServerRole.Admin);

        var about = (await Handler("v0.0.0").Handle(new GetServerAboutQuery(), default)).Value;

        Assert.False(about.IsUpdateAvailable);
    }

    [Fact]
    public async Task With_GitHub_offline_the_latest_version_and_the_update_question_are_unknown()
    {
        _w.SignInAsSomeone(ServerRole.Admin);

        var about = (await Handler(null).Handle(new GetServerAboutQuery(), default)).Value;

        Assert.Null(about.LatestVersion);
        Assert.Null(about.IsUpdateAvailable);
        Assert.Null(about.ReleaseUrl);
        Assert.NotEmpty(about.CurrentVersion);
    }

    [Fact]
    public async Task A_plain_user_is_refused()
    {
        _w.SignInAsSomeone(ServerRole.User);

        var result = await Handler("v999.0.0").Handle(new GetServerAboutQuery(), default);

        Assert.True(result.IsFailed);
        Assert.Equal("forbidden.role", result.Code());
    }
}
