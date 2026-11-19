using HomeMix.Domain.Manifests;

namespace HomeMix.Domain.Tests;

public sealed class DeviceManifestTests
{
    private static string Widget(string devices) =>
        $$"""
        "widgets": [{ "id": "stacja", "title": { "pl": "Stacja" }, "size": "Small", "places": ["Household"], "devices": {{devices}} }]
        """;

    private static FluentResults.Result<AddonManifest> Parse(string devices, string capabilities = """["devices"]""") =>
        AddonManifestParser.Parse(Manifests.Valid(capabilities: capabilities, extra: Widget(devices)));

    [Fact]
    public void Devices_capabilities_are_known_to_the_bridge()
    {
        var manifest = AddonManifestParser.Parse(Manifests.Valid(capabilities: """["devices", "devices.control"]""")).Value;

        Assert.Equal(["devices", "devices.control"], manifest.Capabilities);
    }

    [Fact]
    public void A_widget_lists_how_it_finds_its_devices()
    {
        var manifest = Parse("""
            [ { "capability": "weather" }, { "role": "weather_station" }, { "model": "qwerex56/stacja-v1" },
              { "capability": "qwerex56/akwarium", "pick": "manual" }, { "capability": "qwe-rex/akwarium" } ]
            """).Value;

        var devices = Assert.Single(manifest.Widgets).Devices;
        Assert.Equal(5, devices.Count);
        Assert.Equal("weather", devices[0].Capability);
        Assert.Equal("weather_station", devices[1].Role);
        Assert.Equal("qwerex56/stacja-v1", devices[2].Model);
        Assert.True(devices[3].IsManual);
        Assert.False(devices[0].IsManual);
    }

    [Fact]
    public void A_widget_without_devices_has_an_empty_list()
    {
        var manifest = AddonManifestParser.Parse(Manifests.Valid(
            extra: """ "widgets": [{ "id": "stacja", "title": { "pl": "Stacja" } }] """)).Value;

        Assert.Empty(Assert.Single(manifest.Widgets).Devices);
    }

    [Theory]
    [InlineData("""[ { "pick": "manual" } ]""")]
    [InlineData("""[ {} ]""")]
    [InlineData("""[ { "capability": "Weather" } ]""")]
    [InlineData("""[ { "capability": "qwe-rex/Akwarium" } ]""")]
    [InlineData("""[ { "capability": "-qwe/akwarium" } ]""")]
    [InlineData("""[ { "capability": "weather", "pick": "auto" } ]""")]
    [InlineData("""[ { "role": 5 } ]""")]
    [InlineData("""[ { "model": "has space" } ]""")]
    [InlineData("""[ "weather" ]""")]
    [InlineData("""{ "capability": "weather" }""")]
    public void Bad_matching_is_refused(string devices)
    {
        Assert.True(Parse(devices).IsFailed);
    }

    [Fact]
    public void Matching_devices_needs_the_devices_capability_granted()
    {
        var result = Parse("""[ { "capability": "weather" } ]""", capabilities: "[]");

        Assert.Contains(result.Errors, e => e.Message.Contains("needs the capability devices", StringComparison.Ordinal));
    }

    [Fact]
    public void Control_needs_read()
    {
        var result = AddonManifestParser.Parse(Manifests.Valid(capabilities: """["devices.control"]"""));

        Assert.Contains(result.Errors, e => e.Message.Contains("needs devices as well", StringComparison.Ordinal));
    }

    [Fact]
    public void At_most_eight_entries_per_widget()
    {
        var entries = string.Join(", ", Enumerable.Range(0, 9).Select(i => $$"""{ "model": "a/m{{i}}" }"""));

        Assert.True(Parse($"[{entries}]").IsFailed);
    }
}
