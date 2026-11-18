using Devices.Application.Tests.Fakes;
using Devices.Application.Tools.CallTool;
using Devices.Domain.Devices;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Devices.Application.Tests;

public sealed class CallToolScopeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid _user = Guid.NewGuid();
    private readonly Guid _household = Guid.NewGuid();
    private readonly FakeDevices _devices = new();
    private readonly FakeLinks _links = new();

    private Device Personal(string name, string action)
    {
        var device = Device.Create(_user, name, DeviceKind.Computer, [], Now).Value;
        device.Connect([DeviceAction.Create(action, null, null, false, false).Value], Now);
        _devices.Add(device);
        return device;
    }

    private Device Household(string name, string action)
    {
        var device = Device.AddToHousehold(_household, Guid.NewGuid(), name, "hash", [], Now).Value;
        device.Pair(Now);
        device.Connect([DeviceAction.Create(action, null, null, false, false).Value], Now);
        _devices.Add(device);
        return device;
    }

    private Task<FluentResults.Result<string>> CallAsync(string tool) =>
        new CallToolHandler(_devices, new FakeRoles(HomeApps.Contracts.Households.HouseholdRole.Member, _household), _links, new FakeUser(_user), null!)
            .Handle(new CallToolCommand(tool, null), CancellationToken.None);

    [Fact]
    public async Task A_personal_and_a_household_device_with_the_same_slug_are_both_reachable_each_by_its_own_name()
    {
        var personal = Personal("Lampa", "on");
        var household = Household("Lampa", "on");
        var suffix = household.Id.ToString("N")[..4];

        Assert.True((await CallAsync("lampa_on")).IsSuccess);
        Assert.True((await CallAsync($"lampa-{suffix}_on")).IsSuccess);

        Assert.Equal([personal.Id, household.Id], _links.Runs.Select(r => r.DeviceId));
    }

    [Fact]
    public async Task The_own_device_keeps_the_plain_slug_even_when_the_household_device_is_older()
    {
        var household = Household("Lampa", "on");
        var personal = Personal("Lampa", "on");

        await CallAsync("lampa_on");

        Assert.Equal(personal.Id, Assert.Single(_links.Runs).DeviceId);
        Assert.NotEqual(household.Id, personal.Id);
    }

    [Fact]
    public async Task An_unknown_slug_is_not_found()
    {
        Personal("Lampa", "on");

        var result = await CallAsync("other_on");

        Assert.Equal("devices.not_found", ((AppError)result.Errors[0]).Code);
        Assert.Empty(_links.Runs);
    }
}
