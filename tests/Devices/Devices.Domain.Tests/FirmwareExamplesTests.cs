using System.Text.Json;
using Devices.Domain.Capabilities;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;

namespace Devices.Domain.Tests;

/// <summary>
/// The descriptors compiled into firmware/examples/ (built by the firmware CI for S3, C3 and C6) have to be accepted
/// by the server parser without warnings, and the frames those examples send have to fit them. Keep these copies in
/// step with firmware/examples/*/main/main.c.
/// </summary>
public sealed class FirmwareExamplesTests
{
    private const string Switch =
        """{"format":"homeapps.device/1","model":"example/switch-c3","version":"0.1.0","purpose":"smart_home","roles":["light"],"capabilities":[{"type":"switch"}],"jarvis":"A light on a relay. On or off.","controls":[{"name":"on","label":"Light","value":{"type":"boolean"},"widget":"switch"}]}""";

    private const string WeatherStation =
        """{"format":"homeapps.device/1","model":"example/weather-c6","version":"0.1.0","purpose":"smart_home","roles":["weather_station"],"sleepy":true,"heartbeat":900,"capabilities":[{"type":"weather"},{"type":"power"}],"jarvis":"Outdoor weather station on a battery.","sensors":[{"name":"temperature","label":"Temperature","value":{"type":"number"},"unit":"C"},{"name":"humidity","label":"Humidity","value":{"type":"number","minimum":0,"maximum":100},"unit":"%"},{"name":"battery","label":"Battery","value":{"type":"number","minimum":0,"maximum":100},"unit":"%"}]}""";

    private const string PresenceTag =
        """{"format":"homeapps.device/1","model":"example/presence-tag","version":"0.1.0","purpose":"smart_home","roles":["tracker"],"capabilities":[{"type":"geo"}],"jarvis":"A tag that reports where it is seen.","sensors":[{"name":"location","label":"Location","value":{"type":"object"}}],"events":[{"name":"seen","label":"Seen","data":{"type":"object"},"notify":false}]}""";

    public static TheoryData<string> Examples() => new() { Switch, WeatherStation, PresenceTag };

    [Theory]
    [MemberData(nameof(Examples))]
    public void Each_firmware_example_descriptor_is_accepted_without_warnings(string json)
    {
        var parsed = DescriptorParser.Parse(json);

        Assert.True(parsed.IsSuccess, string.Join("; ", parsed.Errors.Select(e => e.Message)));
        var resolution = CapabilityResolver.Resolve(parsed.Value, CapabilityCatalog.BuiltIn, new Dictionary<string, RegisteredCapability>(), isHouseholdDevice: true);
        Assert.Empty(resolution.Warnings);
        Assert.Equal(parsed.Value.Capabilities.Count, resolution.Accepted.Count);
    }

    [Fact]
    public void The_switch_example_state_frame_fits_its_descriptor()
    {
        var descriptor = DescriptorParser.Parse(Switch).Value;
        using var values = JsonDocument.Parse("""{"on":true}""");

        Assert.True(FrameValidation.CheckState(descriptor, values.RootElement).IsSuccess);
    }

    [Fact]
    public void The_weather_example_state_frame_fits_its_descriptor()
    {
        var descriptor = DescriptorParser.Parse(WeatherStation).Value;
        using var values = JsonDocument.Parse("""{"temperature":21.5,"humidity":48,"battery":80}""");

        Assert.True(FrameValidation.CheckState(descriptor, values.RootElement).IsSuccess);
    }

    [Fact]
    public void The_presence_example_seen_event_fits_its_descriptor()
    {
        var descriptor = DescriptorParser.Parse(PresenceTag).Value;
        using var data = JsonDocument.Parse("""{"where":"hall"}""");

        Assert.True(FrameValidation.CheckEvent(descriptor, "seen", data.RootElement).IsSuccess);
    }
}
