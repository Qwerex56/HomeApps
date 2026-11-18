using Devices.Application.Abstractions;
using Devices.Domain.Devices;

namespace Devices.Application.Common;

internal static class DeviceViews
{
    public static DeviceView ToView(this Device device, IDeviceLinks links) => new(
        device.Id,
        device.Name,
        device.Kind.ToString(),
        !device.IsDisabled && links.IsOnline(device.Id),
        device.IsDisabled,
        device.CreatedAt,
        device.LastSeenAt,
        [.. device.Actions.Select(a => new DeviceActionView(a.Name, a.Description, a.ReadOnly, a.Confirm, a.AutoApprove))]);
}
