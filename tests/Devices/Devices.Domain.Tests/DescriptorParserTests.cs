using Devices.Domain.Descriptors;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Devices.Domain.Tests;

public sealed class DescriptorParserTests
{
    private const string Chair = """
        { "format": "homeapps.device/1", "model": "qwerex56/fotel-v1", "version": "1.2.0", "purpose": "smart_home",
          "roles": ["seat_heater"], "capabilities": [ { "type": "heating" }, { "type": "switch", "id": "lampka" } ],
          "jarvis": "Fotel w salonie.",
          "controls": [
            { "name": "level", "label": "Grzanie", "value": { "type": "integer", "minimum": 0, "maximum": 3 }, "widget": "select" },
            { "name": "lampka.on", "value": { "type": "boolean" }, "widget": "switch" } ],
          "sensors": [ { "name": "temperature", "value": { "type": "number" }, "unit": "°C" } ],
          "events": [ { "name": "button", "data": { "type": "object", "properties": {} }, "notify": true } ],
          "actions": [ { "name": "calibrate", "label": "Kalibruj", "input": { "type": "object", "properties": {} }, "confirm": true } ] }
        """;

    private static readonly string[] Fields = ["format", "model", "version", "purpose"];

    [Fact]
    public void A_full_descriptor_is_parsed()
    {
        var descriptor = DescriptorParser.Parse(Chair).Value;

        Assert.Equal(DevicePurpose.SmartHome, descriptor.Purpose);
        Assert.Equal("qwerex56/fotel-v1", descriptor.Model);
        Assert.Equal(["seat_heater"], descriptor.Roles);
        Assert.Equal(["heating", "switch:lampka"], descriptor.Capabilities.Select(c => c.Key));
        Assert.Equal(2, descriptor.Controls.Count);
        Assert.Single(descriptor.Sensors);
        Assert.True(descriptor.Events[0].Notify);
        Assert.True(descriptor.Actions[0].Confirm);
        Assert.Contains("\"integer\"", descriptor.Controls[0].Value, StringComparison.Ordinal);
    }

    [Fact]
    public void A_sleepy_station_with_a_heartbeat_is_fine()
    {
        var json = """{ "format": "homeapps.device/1", "model": "a/b", "version": "0.1", "purpose": "smart_home", "sleepy": true, "heartbeat": 900 }""";

        var descriptor = DescriptorParser.Parse(json).Value;

        Assert.True(descriptor.Sleepy);
        Assert.Equal(900, descriptor.Heartbeat);
    }

    [Theory]
    [InlineData("""{ "format": "homeapps.device/2" }""", "format")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "A B", "version": "1", "purpose": "assistant" }""", "model")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "one", "purpose": "assistant" }""", "version")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "toaster" }""", "purpose")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "assistant", "roles": ["Bad Role"] }""", "role")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "assistant", "capabilities": [ { "type": "Nope" } ] }""", "capability")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "smart_home", "controls": [ { "name": "x" } ] }""", "JSON Schema")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "smart_home", "controls": [ { "name": "x", "value": { "type": "colour" } } ] }""", "JSON Schema")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "smart_home", "controls": [ { "name": "X", "value": { "type": "boolean" } } ] }""", "name")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "smart_home", "controls": [ { "name": "x", "value": { "type": "boolean" }, "widget": "dial" } ] }""", "widget")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "smart_home", "actions": [ { "name": "go", "input": { "type": "string" } } ] }""", "type object")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "smart_home", "sensors": [ { "name": "t", "value": { "type": "number" } }, { "name": "t", "value": { "type": "number" } } ] }""", "twice")]
    [InlineData("""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "smart_home", "heartbeat": 1 }""", "heartbeat")]
    [InlineData("[]", "object")]
    [InlineData("not json", "JSON")]
    [InlineData("", "JSON")]
    public void A_wrong_descriptor_is_refused_and_says_what_is_wrong(string json, string mention)
    {
        var result = DescriptorParser.Parse(json);

        Assert.True(result.IsFailed);
        Assert.Equal("devices.invalid_descriptor", ((AppError)result.Errors[0]).Code);
        Assert.Contains(mention, result.Errors[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_jarvis_text_is_limited_to_500_characters()
    {
        string With(int length) =>
            $$"""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "assistant", "jarvis": "{{new string('x', length)}}" }""";

        Assert.True(DescriptorParser.Parse(With(500)).IsSuccess);
        Assert.Contains("jarvis", DescriptorParser.Parse(With(501)).Errors[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void All_problems_are_reported_together()
    {
        var result = DescriptorParser.Parse("""{ "format": "x", "model": "A", "version": "v", "purpose": "z" }""");

        foreach (var field in Fields)
        {
            Assert.Contains(field, result.Errors[0].Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_namespaced_capability_brings_its_definition()
    {
        var json = """
            { "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "smart_home",
              "capabilities": [ { "type": "qwerex56/akwarium", "definition": { "version": 2, "fields": [
                { "kind": "sensor", "name": "ph", "value": { "type": "number" } } ] } } ],
              "sensors": [ { "name": "ph", "value": { "type": "number" } } ] }
            """;

        var capability = DescriptorParser.Parse(json).Value.Capabilities.Single();

        Assert.True(capability.IsNamespaced);
        Assert.Equal(2, capability.Definition!.Version);
    }

    [Fact]
    public void A_broken_definition_makes_the_descriptor_invalid()
    {
        var json = """
            { "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "smart_home",
              "capabilities": [ { "type": "qwerex56/akwarium", "definition": { "fields": [ { "kind": "sensor", "name": "ph" } ] } } ] }
            """;

        Assert.True(DescriptorParser.Parse(json).IsFailed);
    }

    [Theory]
    [InlineData("smart_home", true, true)]
    [InlineData("assistant", true, true)]
    [InlineData("computer", true, false)]
    [InlineData("computer", false, true)]
    [InlineData("assistant", false, false)]
    public void A_household_device_is_not_a_computer_and_a_persons_device_is_nothing_else(string purpose, bool household, bool accepted)
    {
        var json = $$"""{ "format": "homeapps.device/1", "model": "a/b", "version": "1", "purpose": "{{purpose}}" }""";
        var descriptor = DescriptorParser.Parse(json).Value;
        var device = household
            ? Device.AddToHousehold(Guid.NewGuid(), Guid.NewGuid(), "x", "hash", [], DateTimeOffset.UnixEpoch).Value
            : Device.Create(Guid.NewGuid(), "x", DeviceKind.Computer, [], DateTimeOffset.UnixEpoch).Value;

        var result = device.ApplyDescriptor(descriptor, new Capabilities.CapabilityResolution([], [], [], []));

        Assert.Equal(accepted, result.IsSuccess);
    }

    [Fact]
    public void A_new_descriptor_replaces_the_old_one_and_a_legacy_device_has_a_purpose_from_its_kind()
    {
        var satellite = Device.AddToHousehold(Guid.NewGuid(), Guid.NewGuid(), "salon", "hash", [], DateTimeOffset.UnixEpoch).Value;
        var computer = Device.Create(Guid.NewGuid(), "mac", DeviceKind.Computer, [], DateTimeOffset.UnixEpoch).Value;
        Assert.Equal(DevicePurpose.Assistant, satellite.Purpose);
        Assert.Equal(DevicePurpose.Computer, computer.Purpose);
        Assert.Null(satellite.Descriptor);

        var none = new Capabilities.CapabilityResolution([], [], [], []);
        satellite.ApplyDescriptor(DescriptorParser.Parse(Chair).Value, none);
        satellite.ApplyDescriptor(DescriptorParser.Parse(Chair.Replace("1.2.0", "1.3.0", StringComparison.Ordinal)).Value, none);

        Assert.Equal("1.3.0", satellite.DescriptorVersion);
        Assert.Equal(DevicePurpose.SmartHome, satellite.Purpose);
    }
}
