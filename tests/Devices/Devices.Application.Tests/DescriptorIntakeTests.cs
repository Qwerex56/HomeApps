using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Capabilities;
using Devices.Domain.Devices;

namespace Devices.Application.Tests;

public sealed class DescriptorIntakeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private const string Weather = """
        { "format": "homeapps.device/1", "model": "a/b", "version": "1.0.0", "purpose": "smart_home",
          "roles": ["weather_station"], "capabilities": [ { "type": "weather" } ],
          "sensors": [ { "name": "temperature", "value": { "type": "number" } }, { "name": "humidity", "value": { "type": "number" } },
                       { "name": "pressure", "value": { "type": "number" } } ] }
        """;

    private static string Aquarium(int version, string extraField) => $$"""
        { "format": "homeapps.device/1", "model": "qwerex56/b", "version": "1.0.0", "purpose": "smart_home",
          "capabilities": [ { "type": "qwerex56/akwarium", "definition": { "version": {{version}}, "fields": [
            { "kind": "sensor", "name": "ph", "value": { "type": "number" } }{{extraField}} ] } } ],
          "sensors": [ { "name": "ph", "value": { "type": "number" } } ] }
        """;

    private readonly FakeRegistry _registry = new();
    private readonly DescriptorIntake _intake;

    public DescriptorIntakeTests() => _intake = new DescriptorIntake(_registry, new FixedTime(Now));

    private static Device HouseholdDevice() =>
        Device.AddToHousehold(Guid.NewGuid(), Guid.NewGuid(), "Stacja", "hash", [], Now).Value;

    [Fact]
    public async Task A_valid_descriptor_is_stored_on_the_device()
    {
        var device = HouseholdDevice();

        var result = await _intake.ApplyAsync(device, Weather, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DevicePurpose.SmartHome, device.Purpose);
        Assert.Equal("a/b", device.Model);
        Assert.Equal(["weather"], device.Capabilities);
        Assert.Equal(["weather_station"], device.Roles);
        Assert.Empty(device.Warnings);
        Assert.NotNull(device.Descriptor);
        Assert.Empty(_registry.Added);
    }

    [Fact]
    public async Task An_invalid_descriptor_fails_and_leaves_the_device_untouched()
    {
        var device = HouseholdDevice();

        var result = await _intake.ApplyAsync(device, """{ "format": "nope" }""", CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Null(device.Descriptor);
        Assert.Empty(_registry.Added);
    }

    [Fact]
    public async Task A_computer_descriptor_on_a_household_device_is_refused()
    {
        var device = HouseholdDevice();
        var json = """{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "computer" }""";

        var result = await _intake.ApplyAsync(device, json, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Null(device.Descriptor);
    }

    [Fact]
    public async Task An_unknown_capability_is_a_warning_and_the_device_still_takes_the_descriptor()
    {
        var device = HouseholdDevice();
        var json = """{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "smart_home", "capabilities": [ { "type": "teleport" } ] }""";

        var result = await _intake.ApplyAsync(device, json, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(device.Capabilities);
        Assert.Single(device.Warnings);
    }

    [Fact]
    public async Task A_namespaced_capability_is_registered_once()
    {
        var first = await _intake.ApplyAsync(HouseholdDevice(), Aquarium(1, ""), CancellationToken.None);
        var second = await _intake.ApplyAsync(HouseholdDevice(), Aquarium(1, ""), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        var registered = Assert.Single(_registry.Added);
        Assert.Equal("qwerex56/akwarium", registered.Name);
        Assert.Equal(1, registered.Version);
    }

    [Fact]
    public async Task A_namespaced_capability_of_another_author_is_ignored_with_a_warning()
    {
        var device = HouseholdDevice();

        var result = await _intake.ApplyAsync(device, Aquarium(1, "").Replace("qwerex56/b", "other/b", StringComparison.Ordinal), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(device.Capabilities);
        Assert.Single(device.Warnings);
        Assert.Empty(_registry.Added);
    }

    [Fact]
    public async Task A_hidden_capability_is_ignored_with_a_warning()
    {
        await _intake.ApplyAsync(HouseholdDevice(), Aquarium(1, ""), CancellationToken.None);
        _registry.Added.Single().Hide();
        var device = HouseholdDevice();

        await _intake.ApplyAsync(device, Aquarium(1, ""), CancellationToken.None);

        Assert.Empty(device.Capabilities);
        Assert.Single(device.Warnings);
    }

    [Fact]
    public async Task Another_author_uses_the_registered_definition_and_does_not_extend_it()
    {
        await _intake.ApplyAsync(HouseholdDevice(), Aquarium(1, ""), CancellationToken.None);
        var extra = """, { "kind": "sensor", "name": "temp", "value": { "type": "number" }, "required": false }""";
        var device = HouseholdDevice();

        await _intake.ApplyAsync(device, Aquarium(2, extra).Replace("qwerex56/b", "other/b", StringComparison.Ordinal), CancellationToken.None);

        Assert.Equal(["qwerex56/akwarium"], device.Capabilities);
        Assert.Equal(1, Assert.Single(_registry.Added).Version);
    }

    [Fact]
    public async Task A_newer_definition_extends_the_registered_one()
    {
        await _intake.ApplyAsync(HouseholdDevice(), Aquarium(1, ""), CancellationToken.None);
        var later = new DescriptorIntake(_registry, new FixedTime(Now.AddDays(1)));
        var extra = """, { "kind": "sensor", "name": "temp", "value": { "type": "number" }, "required": false }""";

        await later.ApplyAsync(HouseholdDevice(), Aquarium(2, extra), CancellationToken.None);

        var registered = Assert.Single(_registry.Added);
        Assert.Equal(2, registered.Version);
        Assert.Equal(Now.AddDays(1), registered.UpdatedAt);
        Assert.Contains(registered.Definition.Fields, f => f.Name == "temp");
    }

    [Fact]
    public async Task A_descriptor_sent_again_replaces_the_stored_one()
    {
        var device = HouseholdDevice();
        await _intake.ApplyAsync(device, Weather, CancellationToken.None);

        await _intake.ApplyAsync(device, Weather.Replace("1.0.0", "2.0.0", StringComparison.Ordinal), CancellationToken.None);

        Assert.Equal("2.0.0", device.DescriptorVersion);
    }

    private sealed class FakeRegistry : ICapabilityRegistry
    {
        public List<RegisteredCapability> Added { get; } = [];

        public Task<RegisteredCapability?> FindAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult(Added.Find(c => c.Name == name));

        public Task<IReadOnlyList<RegisteredCapability>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RegisteredCapability>>(Added);

        public void Add(RegisteredCapability capability) => Added.Add(capability);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
