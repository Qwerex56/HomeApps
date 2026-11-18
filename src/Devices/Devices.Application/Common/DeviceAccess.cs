using FluentResults;
using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.Common;

internal static class DeviceAccess
{
    /// <summary>
    /// A device the caller may change: their own computer, or a household device when they are an admin there. Someone
    /// else's device, or one of a household they are not in, is "not found", like one that does not exist.
    /// </summary>
    public static async Task<Result<Device>> FindManageableAsync(
        this IDeviceRepository devices, IHouseholdRoles households, Guid deviceId, Guid userId, CancellationToken cancellationToken)
    {
        var device = await devices.FindAsync(deviceId, cancellationToken);
        if (device is null)
        {
            return Result.Fail(DeviceErrors.NotFound);
        }

        if (device.HouseholdId is not { } householdId)
        {
            return device.OwnerId == userId ? Result.Ok(device) : Result.Fail(DeviceErrors.NotFound);
        }

        return await households.GetRoleAsync(householdId, cancellationToken) switch
        {
            null => Result.Fail(DeviceErrors.NotFound),
            < HouseholdRole.Admin => Result.Fail(DeviceErrors.Forbidden),
            _ => Result.Ok(device),
        };
    }
}
