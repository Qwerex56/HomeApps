using Devices.Application.HouseholdDevices.GetHouseholdDevices;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.Tests;

public sealed class HouseholdDeviceFilterTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Household = Guid.NewGuid();

    private const string Station = """
        { "format": "homeapps.device/1", "model": "a/stacja", "version": "1.0.0", "purpose": "smart_home",
          "roles": ["weather_station"], "capabilities": [ { "type": "weather" }, { "type": "power" } ],
          "sensors": [ { "name": "temperature", "value": { "type": "number" } }, { "name": "humidity", "value": { "type": "number" } },
                       { "name": "pressure", "value": { "type": "number" } }, { "name": "battery", "value": { "type": "number" } },
                       { "name": "voltage", "value": { "type": "number" } }, { "name": "source", "value": { "type": "string" } } ] }
        """;

    private readonly FakeDevices _devices = new();
    private readonly FakeTelemetry _telemetry = new();
    private readonly Device _chair = PairedDevice.Create(Now, PairedDevice.Chair, Household);
    private readonly Device _station = PairedDevice.Create(Now, Station, Household);

    public HouseholdDeviceFilterTests()
    {
        _devices.Add(_chair);
        _devices.Add(_station);
    }

    private async Task<IReadOnlyList<Devices.Application.Common.DeviceView>> ListAsync(GetHouseholdDevicesQuery query, HouseholdRole role = HouseholdRole.Guest) =>
        (await new GetHouseholdDevicesHandler(_devices, new FakeRoles(role), new FakeLinks(), _telemetry).Handle(query, CancellationToken.None)).Value;

    [Fact]
    public async Task Without_filters_every_device_is_listed_without_state()
    {
        var list = await ListAsync(new GetHouseholdDevicesQuery(Household));

        Assert.Equal(2, list.Count);
        Assert.All(list, d => Assert.Null(d.State));
    }

    [Theory]
    [InlineData("weather", null, null, 1)]
    [InlineData(null, "weather_station", null, 1)]
    [InlineData(null, null, "a/fotel", 1)]
    [InlineData("weather", "weather_station", "a/fotel", 0)]
    [InlineData("switch", null, null, 0)]
    public async Task Filters_narrow_by_capability_role_and_model(string? capability, string? role, string? model, int expected)
    {
        var list = await ListAsync(new GetHouseholdDevicesQuery(Household, capability, role, model));

        Assert.Equal(expected, list.Count);
    }

    [Fact]
    public async Task State_is_attached_on_request_and_a_guest_may_read_it()
    {
        var list = await ListAsync(new GetHouseholdDevicesQuery(Household, Capability: "weather", WithState: true), HouseholdRole.Guest);

        Assert.NotNull(Assert.Single(list).State);
    }
}
