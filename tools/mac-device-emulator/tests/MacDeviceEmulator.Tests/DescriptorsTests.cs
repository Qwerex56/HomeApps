using System.Text.Json;
using MacDeviceEmulator.Protocol;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class DescriptorsTests
{
    [Theory]
    [InlineData("satellite")]
    [InlineData("powered")]
    [InlineData("battery")]
    [InlineData("computer")]
    public void All_descriptors_have_valid_schema_properties_and_model_syntax(string kind)
    {
        var desc = kind switch
        {
            "satellite" => Descriptors.SatelliteDescriptor,
            "powered" => Descriptors.PoweredDescriptor,
            "battery" => Descriptors.BatteryDescriptor,
            "computer" => Descriptors.ComputerDescriptor,
            _ => throw new ArgumentException()
        };

        using var doc = JsonDocument.Parse(desc);
        var root = doc.RootElement;

        Assert.Equal("homeapps.device/1", root.GetProperty("format").GetString());

        var model = root.GetProperty("model").GetString();
        Assert.NotNull(model);
        Assert.Matches(@"^[a-z0-9][a-z0-9._-]*(/[a-z0-9][a-z0-9._-]*)?$", model);

        var version = root.GetProperty("version").GetString();
        Assert.NotNull(version);
        Assert.Matches(@"^\d{1,4}(\.\d{1,4}){0,2}([-+][0-9A-Za-z.-]{1,30})?$", version);

        var purpose = root.GetProperty("purpose").GetString();
        Assert.Contains(purpose, new[] { "assistant", "smart_home", "computer" });

        var jarvis = root.GetProperty("jarvis").GetString();
        Assert.NotNull(jarvis);
        Assert.True(jarvis.Length <= 500);

        if (root.TryGetProperty("heartbeat", out var hb))
        {
            Assert.InRange(hb.GetInt32(), 5, 86400);
        }

        if (root.TryGetProperty("controls", out var controls))
        {
            foreach (var c in controls.EnumerateArray())
            {
                var name = c.GetProperty("name").GetString();
                Assert.NotNull(name);
                Assert.Matches(@"^[a-z][a-z0-9_]{0,29}(\.[a-z][a-z0-9_]{0,29})?$", name);
                Assert.True(c.GetProperty("value").TryGetProperty("type", out _));
            }
        }

        if (root.TryGetProperty("sensors", out var sensors))
        {
            foreach (var s in sensors.EnumerateArray())
            {
                var name = s.GetProperty("name").GetString();
                Assert.NotNull(name);
                Assert.Matches(@"^[a-z][a-z0-9_]{0,29}(\.[a-z][a-z0-9_]{0,29})?$", name);
                Assert.True(s.GetProperty("value").TryGetProperty("type", out _));
            }
        }
    }

    [Fact]
    public void Satellite_descriptor_has_voice_and_audio_capabilities_and_say_action()
    {
        using var doc = JsonDocument.Parse(Descriptors.SatelliteDescriptor);
        var root = doc.RootElement;

        Assert.Equal("assistant", root.GetProperty("purpose").GetString());

        var caps = root.GetProperty("capabilities").EnumerateArray()
            .Select(c => c.GetProperty("type").GetString())
            .ToList();
        Assert.Contains("voice", caps);
        Assert.Contains("audio", caps);

        var actions = root.GetProperty("actions").EnumerateArray()
            .Select(a => a.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("say", actions);
    }

    [Fact]
    public void Powered_descriptor_has_audio_and_switch_and_sensors()
    {
        using var doc = JsonDocument.Parse(Descriptors.PoweredDescriptor);
        var root = doc.RootElement;

        Assert.Equal("smart_home", root.GetProperty("purpose").GetString());
        Assert.Equal(60, root.GetProperty("heartbeat").GetInt32());

        var controls = root.GetProperty("controls").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("volume", controls);
        Assert.Contains("muted", controls);
        Assert.Contains("lamp.on", controls);

        var sensors = root.GetProperty("sensors").EnumerateArray()
            .Select(s => s.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("cpu_load", sensors);
    }

    [Fact]
    public void Battery_descriptor_is_sleepy_with_power_and_battery_sensor()
    {
        using var doc = JsonDocument.Parse(Descriptors.BatteryDescriptor);
        var root = doc.RootElement;

        Assert.Equal("smart_home", root.GetProperty("purpose").GetString());
        Assert.True(root.GetProperty("sleepy").GetBoolean());
        Assert.Equal(60, root.GetProperty("heartbeat").GetInt32());

        var sensors = root.GetProperty("sensors").EnumerateArray()
            .Select(s => s.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("battery", sensors);
    }

    [Fact]
    public void Computer_descriptor_has_purpose_computer_and_audio_control()
    {
        using var doc = JsonDocument.Parse(Descriptors.ComputerDescriptor);
        var root = doc.RootElement;

        Assert.Equal("computer", root.GetProperty("purpose").GetString());

        var controls = root.GetProperty("controls").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("volume", controls);
        Assert.Contains("muted", controls);
    }
}
