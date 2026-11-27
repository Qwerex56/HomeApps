using System.Text.Json;
using Devices.Application.Controls.SetControl;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.Tests;

public sealed class SetControlTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeDevices _devices = new();
    private readonly FakeLinks _links = new();
    private readonly FakeTelemetry _telemetry = new();
    private readonly FakeStream _stream = new();
    private readonly Device _device = PairedDevice.Create(Now);

    public SetControlTests() => _devices.Add(_device);

    private Task<FluentResults.Result> SetAsync(HouseholdRole? role, string control, string value) =>
        new SetControlHandler(_devices, new FakeRoles(role), _links, _telemetry, _stream)
            .Handle(new SetControlCommand(_device.Id, control, JsonElement.Parse(value)), CancellationToken.None);

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Admin)]
    [InlineData(HouseholdRole.Owner)]
    public async Task A_member_or_more_sets_a_control_and_the_device_gets_it(HouseholdRole role)
    {
        var result = await SetAsync(role, "heating", "3");

        Assert.True(result.IsSuccess);
        Assert.Equal((_device.Id, "heating", "3"), Assert.Single(_links.Sets));
    }

    [Fact]
    public async Task A_guest_only_looks()
    {
        var result = await SetAsync(HouseholdRole.Guest, "heating", "3");

        Assert.Equal("devices.read_only", ((HomeApps.BuildingBlocks.Domain.Errors.AppError)result.Errors[0]).Code);
        Assert.Empty(_links.Sets);
    }

    [Fact]
    public async Task Someone_outside_the_household_finds_nothing()
    {
        var result = await SetAsync(null, "heating", "3");

        Assert.Equal("devices.not_found", ((HomeApps.BuildingBlocks.Domain.Errors.AppError)result.Errors[0]).Code);
    }

    [Theory]
    [InlineData("heating", "4")]
    [InlineData("heating", "\"hot\"")]
    [InlineData("temperature", "1")]
    [InlineData("nope", "1")]
    public async Task A_value_that_breaks_the_control_schema_never_reaches_the_device(string control, string value)
    {
        var result = await SetAsync(HouseholdRole.Member, control, value);

        Assert.True(result.IsFailed);
        Assert.Empty(_links.Sets);
    }

    [Fact]
    public async Task An_offline_device_cannot_be_set()
    {
        _links.Online = false;

        var result = await SetAsync(HouseholdRole.Member, "heating", "1");

        Assert.Equal("devices.offline", ((HomeApps.BuildingBlocks.Domain.Errors.AppError)result.Errors[0]).Code);
    }
}
