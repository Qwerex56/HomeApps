using Devices.Domain.Capabilities;
using Devices.Domain.Descriptors;

namespace Devices.Domain.Tests;

public sealed class CapabilityTests
{
    private static CapabilityResolution Resolve(
        string body, string model = "qwerex56/b", RegisteredCapability? registered = null, bool household = true) => CapabilityResolver.Resolve(
        DescriptorParser.Parse($$"""{ "format": "homeapps.device/1", "model": "{{model}}", "version": "1", "purpose": "smart_home", {{body}} }""").Value,
        CapabilityCatalog.BuiltIn,
        registered is null ? new Dictionary<string, RegisteredCapability>() : new Dictionary<string, RegisteredCapability> { [registered.Name] = registered },
        household);

    private const string Aquarium = """
        "capabilities": [ { "type": "qwerex56/akwarium", "definition": { "version": 2, "fields": [
          { "kind": "sensor", "name": "ph", "value": { "type": "number" } },
          { "kind": "sensor", "name": "temp", "value": { "type": "number" }, "required": false } ] } } ],
        "sensors": [ { "name": "ph", "value": { "type": "number" } } ]
        """;

    private static List<string> ParseProblems(string json)
    {
        var problems = new List<string>();
        using var document = System.Text.Json.JsonDocument.Parse(json);
        CapabilityDefinition.Parse(document.RootElement, "a/b", problems);
        return problems;
    }

    [Fact]
    public void The_built_in_catalog_is_loaded_from_the_embedded_json()
    {
        var names = CapabilityCatalog.BuiltIn.Capabilities.Select(c => c.Name);

        Assert.Equal(["audio", "button", "geo", "heating", "level", "power", "presence", "switch", "voice", "weather"], names.Order());
        Assert.True(CapabilityCatalog.BuiltIn.HasRole("weather_station"));
        Assert.False(CapabilityCatalog.BuiltIn.HasRole("toaster"));
    }

    [Fact]
    public void A_weather_station_with_power_has_both_capabilities()
    {
        var resolution = Resolve("""
            "roles": ["weather_station"], "capabilities": [ { "type": "weather" }, { "type": "power" } ],
            "sensors": [ { "name": "temperature", "value": { "type": "number" } }, { "name": "battery", "value": { "type": "integer" } } ]
            """);

        Assert.Equal(["weather", "power"], resolution.Accepted);
        Assert.Equal(["weather_station"], resolution.Roles);
        Assert.Empty(resolution.Warnings);
    }

    [Fact]
    public void An_instance_id_prefixes_the_fields()
    {
        var resolution = Resolve("""
            "capabilities": [ { "type": "switch", "id": "lampka" }, { "type": "switch", "id": "gniazdko" } ],
            "controls": [ { "name": "lampka.on", "value": { "type": "boolean" } } ]
            """);

        Assert.Equal(["switch:lampka"], resolution.Accepted);
        Assert.Contains("switch:gniazdko", resolution.Warnings.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_capability_is_ignored_with_a_warning_and_so_is_an_unknown_role()
    {
        var resolution = Resolve(""" "roles": ["toaster"], "capabilities": [ { "type": "teleport" }, { "type": "voice" } ] """);

        Assert.Equal(["voice"], resolution.Accepted);
        Assert.Empty(resolution.Roles);
        Assert.Equal(2, resolution.Warnings.Count);
        Assert.Contains("teleport", resolution.Warnings[0], StringComparison.Ordinal);
    }

    [Fact]
    public void A_capability_whose_fields_the_device_lacks_or_types_wrongly_is_ignored()
    {
        var missing = Resolve(""" "capabilities": [ { "type": "switch" } ] """);
        var wrongType = Resolve("""
            "capabilities": [ { "type": "switch" } ], "controls": [ { "name": "on", "value": { "type": "string" } } ]
            """);
        var noneOfAny = Resolve(""" "capabilities": [ { "type": "weather" } ], "sensors": [ { "name": "rain", "value": { "type": "number" } } ] """);

        Assert.Empty(missing.Accepted);
        Assert.Empty(wrongType.Accepted);
        Assert.Empty(noneOfAny.Accepted);
        Assert.Contains("any of", noneOfAny.Warnings.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_namespaced_capability_with_a_definition_is_accepted_and_to_be_registered()
    {
        var resolution = Resolve("""
            "capabilities": [ { "type": "qwerex56/akwarium", "definition": { "version": 1, "fields": [
              { "kind": "sensor", "name": "ph", "value": { "type": "number" } } ] } } ],
            "sensors": [ { "name": "ph", "value": { "type": "number" } } ]
            """);

        Assert.Equal(["qwerex56/akwarium"], resolution.Accepted);
        Assert.Equal("qwerex56/akwarium", resolution.ToRegister.Single().Name);
        Assert.Empty(resolution.Warnings);
    }

    [Fact]
    public void A_namespaced_capability_without_a_definition_is_ignored_with_a_warning()
    {
        var resolution = Resolve(""" "capabilities": [ { "type": "qwerex56/akwarium" } ] """);

        Assert.Empty(resolution.Accepted);
        Assert.Empty(resolution.ToRegister);
        Assert.Contains("definition", resolution.Warnings.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_registered_definition_wins_over_the_one_the_device_brings()
    {
        var registered = RegisteredCapability.Register(Definition(1, "ph", "temp"), DateTimeOffset.UnixEpoch);
        var body = """
            "capabilities": [ { "type": "qwerex56/akwarium", "definition": { "version": 1, "fields": [ { "kind": "sensor", "name": "x", "value": { "type": "number" } } ] } } ],
            "sensors": [ { "name": "ph", "value": { "type": "number" } }, { "name": "temp", "value": { "type": "number" } } ]
            """;

        var stranger = Resolve(body, "other/device", registered);
        var author = Resolve(body, "qwerex56/device", registered);

        Assert.Equal(["qwerex56/akwarium"], stranger.Accepted);
        Assert.Empty(stranger.ToRegister);
        Assert.Equal(["qwerex56/akwarium"], author.Accepted);
        Assert.Single(author.ToRegister);
    }

    [Fact]
    public void A_stranger_with_a_registered_definition_needs_the_registered_fields_not_its_own()
    {
        var registered = RegisteredCapability.Register(Definition(1, "ph"), DateTimeOffset.UnixEpoch);
        var body = """
            "capabilities": [ { "type": "qwerex56/akwarium", "definition": { "version": 1, "fields": [ { "kind": "sensor", "name": "x", "value": { "type": "number" } } ] } } ],
            "sensors": [ { "name": "x", "value": { "type": "number" } } ]
            """;

        var resolution = Resolve(body, "other/device", registered);

        Assert.Empty(resolution.Accepted);
        Assert.Contains("ph", resolution.Warnings.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_hidden_registered_capability_is_ignored_with_a_warning()
    {
        var registered = RegisteredCapability.Register(Definition(1, "ph"), DateTimeOffset.UnixEpoch);
        registered.Hide();

        var resolution = Resolve(Aquarium, "qwerex56/b", registered);

        Assert.Empty(resolution.Accepted);
        Assert.Empty(resolution.ToRegister);
        Assert.Contains("hid", resolution.Warnings.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_unregistered_capability_of_another_author_is_ignored()
    {
        var resolution = Resolve(Aquarium, "other/device");

        Assert.Empty(resolution.Accepted);
        Assert.Empty(resolution.ToRegister);
        Assert.Single(resolution.Warnings);
    }

    [Fact]
    public void An_unregistered_capability_is_ignored_when_the_device_is_not_a_household_device()
    {
        var resolution = Resolve(Aquarium, household: false);

        Assert.Empty(resolution.Accepted);
        Assert.Empty(resolution.ToRegister);
        Assert.Single(resolution.Warnings);
    }

    [Fact]
    public void A_definition_needs_a_field_and_a_sane_version()
    {
        Assert.Contains("at least one field", ParseProblems("""{ "version": 1, "fields": [] }""").Single(), StringComparison.Ordinal);
        Assert.NotEmpty(ParseProblems("""{ "version": 10001, "fields": [ { "kind": "sensor", "name": "ph" } ] }"""));
        Assert.Empty(ParseProblems("""{ "version": 10000, "fields": [ { "kind": "sensor", "name": "ph" } ] }"""));
    }

    [Fact]
    public void A_definition_with_a_non_string_anyOf_is_a_problem_not_an_exception()
    {
        var problems = ParseProblems("""{ "version": 1, "anyOf": [1, null], "fields": [ { "kind": "sensor", "name": "ph" } ] }""");

        Assert.Contains("anyOf is a list of field names", problems.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_definition_caps_anyOf_and_the_description()
    {
        var many = string.Join(",", Enumerable.Repeat("\"ph\"", CapabilityDefinition.MaxFields + 1));
        Assert.NotEmpty(ParseProblems($$"""{ "version": 1, "anyOf": [{{many}}], "fields": [ { "kind": "sensor", "name": "ph" } ] }"""));

        var longText = new string('x', 301);
        Assert.NotEmpty(ParseProblems($$"""{ "version": 1, "description": "{{longText}}", "fields": [ { "kind": "sensor", "name": "ph" } ] }"""));
    }

    [Fact]
    public void A_newer_version_only_extends_a_registered_capability()
    {
        var v1 = Definition(1, "ph");
        var registered = RegisteredCapability.Register(v1, DateTimeOffset.UnixEpoch);

        registered.Extend(Definition(1, "ph", "temp"), DateTimeOffset.UnixEpoch.AddDays(1));
        Assert.Single(registered.Definition.Fields);

        registered.Extend(Definition(2, "ph", "temp"), DateTimeOffset.UnixEpoch.AddDays(2));
        Assert.Equal(2, registered.Version);
        Assert.Equal(["ph", "temp"], registered.Definition.Fields.Select(f => f.Name));

        registered.Extend(Definition(3, "temp"), DateTimeOffset.UnixEpoch.AddDays(3));
        Assert.Equal(["ph", "temp"], registered.Definition.Fields.Select(f => f.Name));
    }

    [Fact]
    public void A_registered_capability_can_be_hidden_and_shown()
    {
        var registered = RegisteredCapability.Register(Definition(1, "ph"), DateTimeOffset.UnixEpoch);

        registered.Hide();
        Assert.True(registered.Hidden);
        registered.Unhide();
        Assert.False(registered.Hidden);
    }

    [Fact]
    public void An_extension_past_the_field_limit_is_ignored_with_a_warning()
    {
        var names = Enumerable.Range(0, CapabilityDefinition.MaxFields).Select(i => $"s{i}").ToArray();
        var registered = RegisteredCapability.Register(Definition(1, names), DateTimeOffset.UnixEpoch);
        var body = """
            "capabilities": [ { "type": "qwerex56/akwarium", "definition": { "version": 2, "fields": [ { "kind": "sensor", "name": "extra", "value": { "type": "number" } } ] } } ],
            "sensors": [ { "name": "s0", "value": { "type": "number" } } ]
            """;

        var resolution = Resolve(body, "qwerex56/b", registered);

        Assert.Empty(resolution.ToRegister);
        Assert.Contains(resolution.Warnings, w => w.Contains("extension ignored: more than 30 fields", StringComparison.Ordinal));
    }

    [Fact]
    public void A_personal_device_never_extends_a_registered_capability()
    {
        var registered = RegisteredCapability.Register(Definition(1, "ph"), DateTimeOffset.UnixEpoch);
        var body = """
            "capabilities": [ { "type": "qwerex56/akwarium", "definition": { "version": 2, "fields": [ { "kind": "sensor", "name": "x", "value": { "type": "number" } } ] } } ],
            "sensors": [ { "name": "x", "value": { "type": "number" } } ]
            """;

        var resolution = Resolve(body, "qwerex56/b", registered, household: false);

        Assert.Empty(resolution.ToRegister);
        Assert.Empty(resolution.Accepted);
        Assert.Contains("ph", resolution.Warnings.Single(), StringComparison.Ordinal);
    }

    private static CapabilityDefinition Definition(int version, params string[] sensors) =>
        new("qwerex56/akwarium", version, null, [.. sensors.Select(s => new CapabilityField(CapabilityFieldKind.Sensor, s, """{"type":"number"}""", true))], []);
}
