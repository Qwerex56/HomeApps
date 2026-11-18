using System.Text.Json;
using FluentResults;

namespace HomeApps.Plugins.Devices;

/// <summary>
/// A household's devices as a server plugin sees them (docs/plugins.md, "Devices"): list them by what they can do, read
/// their last state and change a control. Every call carries the caller's own access token, so Devices applies its own
/// rules (guests read, members control). Works inside a request, like <see cref="IHouseholdAccess"/>; to react to a
/// device without a request, subscribe to <c>DeviceStateChanged</c> or <c>DeviceEventOccurred</c> instead.
/// </summary>
public interface IDevicesClient
{
    /// <summary>
    /// The household's devices that are not removed and match <paramref name="filter"/> (all of its parts, none means
    /// every device); with <paramref name="withState"/> each carries its last state. Empty without access.
    /// </summary>
    Task<IReadOnlyList<PluginDevice>> ListAsync(Guid householdId, DeviceFilter? filter, bool withState, CancellationToken cancellationToken);

    /// <summary>A device's last state, or <c>null</c> when it is not there or the caller may not see it.</summary>
    Task<PluginDeviceState?> GetStateAsync(Guid deviceId, CancellationToken cancellationToken);

    /// <summary>Changes a control (member and above); fails with Devices' own error, e.g. <c>devices.offline</c>.</summary>
    Task<Result> SetAsync(Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken);
}
