using System.Text.Json;
using MacDeviceEmulator.Protocol;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class DescriptorsTests
{
    [Fact]
    public void All_descriptors_are_valid_json_and_conform_to_homeapps_device_1()
    {
        var descriptors = new[]
        {
            Descriptors.SatelliteDescriptor,
            Descriptors.PoweredDescriptor,
            Descriptors.BatteryDescriptor,
            Descriptors.ComputerDescriptor
        };

        foreach (var desc in descriptors)
        {
            using var doc = JsonDocument.Parse(desc);
            var root = doc.RootElement;

            Assert.Equal("homeapps.device/1", root.GetProperty("format").GetString());
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("model").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("purpose").GetString()));
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
