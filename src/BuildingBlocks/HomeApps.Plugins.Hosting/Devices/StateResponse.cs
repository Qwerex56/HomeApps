using System.Text.Json;
using HomeApps.Plugins.Devices;

namespace HomeApps.Plugins.Hosting.Devices;

internal sealed record StateResponse(Guid DeviceId, JsonElement Values, DateTimeOffset? ValuesAt, bool Online, bool Silent, bool LowBattery, double? Battery)
{
    public PluginDeviceState ToState() => new(DeviceId, Values, ValuesAt, Online, Silent, LowBattery, Battery);
}
