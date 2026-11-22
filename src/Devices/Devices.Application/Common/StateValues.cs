using System.Text.Json;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.Common;

/// <summary>
/// The one place that decides what a reader sees of <see cref="DeviceState.Values"/> (R3): a household's location device
/// (capability <c>geo</c>) shows its values only to Member and up; a guest gets <c>{}</c> and no time. Health (online, silent,
/// battery, rssi) is not touched. A person's own device is for its owner alone, so it is always shown.
/// </summary>
internal static class StateValues
{
    public static bool VisibleTo(Device device, HouseholdRole? role) =>
        !device.IsHouseholdDevice || !device.IsLocationDevice || role >= HouseholdRole.Member;

    public static (JsonElement Values, DateTimeOffset? At) Of(Device device, DeviceState? state, HouseholdRole? role)
    {
        using var values = JsonDocument.Parse(VisibleTo(device, role) ? state?.Values ?? "{}" : "{}");
        return (values.RootElement.Clone(), VisibleTo(device, role) ? state?.ValuesAt : null);
    }
}
