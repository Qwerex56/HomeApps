using Devices.Domain.Devices;

namespace Devices.Application.Common;

/// <summary>
/// How add-ons and plugins find devices (household-jarvis-devices.md 2.5): by what they can do (a capability, with or
/// without an instance id: <c>switch</c> matches <c>switch:lampka</c>), by role, by model. Every filter given must match.
/// </summary>
public sealed record DeviceFilter(string? Capability, string? Role, string? Model)
{
    public bool IsEmpty => Capability is null && Role is null && Model is null;

    public bool Matches(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);
        return (Capability is null || device.Capabilities.Any(c => string.Equals(c, Capability, StringComparison.Ordinal)
                    || c.StartsWith(Capability + ":", StringComparison.Ordinal)))
            && (Role is null || device.Roles.Contains(Role, StringComparer.Ordinal))
            && (Model is null || string.Equals(device.Model, Model, StringComparison.Ordinal));
    }
}
