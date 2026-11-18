using Devices.Application.Abstractions;
using Devices.Domain.Devices;

namespace Devices.Application.Common;

internal static class DeviceViews
{
    public static DeviceView ToView(this Device device, IDeviceLinks links, bool includeNotes = false) => new(
        device.Id,
        device.Name,
        device.Slug,
        device.Kind.ToString(),
        device.Purpose.ToWire(),
        device.HouseholdId,
        !device.IsHouseholdDevice || device.PairedAt is not null,
        !device.IsDisabled && links.IsOnline(device.Id),
        device.IsDisabled,
        device.CreatedAt,
        device.LastSeenAt,
        [.. device.Actions.Select(a => new DeviceActionView(a.Name, a.Description, a.ReadOnly, a.Confirm, a.AutoApprove))],
        new DeviceDescriptorView(device.Descriptor is not null, device.Model, device.DescriptorVersion, device.Roles, device.Capabilities, device.Warnings),
        includeNotes ? device.AdminNotes : null,
        device.JarvisMayControl,
        device.JarvisAsks);
}
