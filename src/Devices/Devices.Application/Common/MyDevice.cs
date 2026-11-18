using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using FluentResults;

namespace Devices.Application.Common;

internal static class MyDevice
{
    /// <summary>The device when it is the caller's; another person's device is "not found", like one that does not exist.</summary>
    public static async Task<Result<Device>> FindMineAsync(this IDeviceRepository devices, Guid deviceId, Guid ownerId, CancellationToken cancellationToken)
    {
        var device = await devices.FindAsync(deviceId, cancellationToken);
        return device is null || device.OwnerId != ownerId ? Result.Fail(DeviceErrors.NotFound) : Result.Ok(device);
    }
}
