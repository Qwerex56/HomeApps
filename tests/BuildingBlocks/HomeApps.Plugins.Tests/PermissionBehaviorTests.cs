using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Contracts.Households;
using MediatR;
using NSubstitute;

namespace HomeApps.Plugins.Tests;

public sealed class PermissionBehaviorTests
{
    private static readonly Guid Household = Guid.NewGuid();

    private readonly IHouseholdAccess _access = Substitute.For<IHouseholdAccess>();
    private readonly IPluginEnablement _enablement = Substitute.For<IPluginEnablement>();
    private int _handled;

    private sealed record Write(Guid HouseholdId, string Permission = "tasks.write") : IRequest<Result>, IHouseholdRequest;

    private sealed record Other : IRequest<Result>;

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Owner)]
    public async Task A_member_with_enough_role_gets_through(HouseholdRole role)
    {
        Given(role, enabled: true);

        var result = await Run(new Write(Household));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _handled);
    }

    [Fact]
    public async Task Someone_outside_the_household_learns_nothing()
    {
        Given(null, enabled: true);

        var result = await Run(new Write(Household));

        Assert.Equal("household.not_found", Code(result));
        await _enablement.DidNotReceiveWithAnyArgs().IsEnabledAsync(default, default);
    }

    [Fact]
    public async Task A_switched_off_plugin_says_so()
    {
        Given(HouseholdRole.Owner, enabled: false);

        Assert.Equal("plugin.disabled", Code(await Run(new Write(Household))));
    }

    [Fact]
    public async Task A_guest_may_not_write()
    {
        Given(HouseholdRole.Guest, enabled: true);

        var result = await Run(new Write(Household));

        Assert.Equal("plugin.forbidden", Code(result));
        Assert.Equal(ErrorKind.Forbidden, Assert.IsType<AppError>(result.Errors[0]).Kind);
        Assert.Equal(0, _handled);
    }

    [Fact]
    public async Task A_permission_missing_from_the_manifest_is_a_bug()
    {
        Given(HouseholdRole.Owner, enabled: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Run(new Write(Household, "tasks.delete")));
    }

    [Fact]
    public async Task Requests_about_no_household_are_not_checked()
    {
        var behavior = new HouseholdPermissionBehavior<Other, Result>(TestManifests.Valid, _access, _enablement);

        var result = await behavior.Handle(new Other(), _ => Task.FromResult(Result.Ok()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _access.DidNotReceiveWithAnyArgs().GetRoleAsync(default, default);
    }

    private void Given(HouseholdRole? role, bool enabled)
    {
        _access.GetRoleAsync(Household, Arg.Any<CancellationToken>()).Returns(role);
        _enablement.IsEnabledAsync(Household, Arg.Any<CancellationToken>()).Returns(enabled);
    }

    private Task<Result> Run(Write request)
    {
        var behavior = new HouseholdPermissionBehavior<Write, Result>(TestManifests.Valid, _access, _enablement);
        return behavior.Handle(request, _ =>
        {
            _handled++;
            return Task.FromResult(Result.Ok());
        }, CancellationToken.None);
    }

    private static string? Code(ResultBase result) => (result.Errors.SingleOrDefault() as AppError)?.Code;
}
