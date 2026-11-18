using System.Text.Json;

namespace HomeApps.Plugins.Devices;

/// <summary>A device's last values (control and sensor names) with the time they were read, and whether it is online, silent or low on battery.</summary>
public sealed record PluginDeviceState(
    Guid DeviceId, JsonElement Values, DateTimeOffset? ValuesAt, bool Online, bool Silent, bool LowBattery, double? Battery);
