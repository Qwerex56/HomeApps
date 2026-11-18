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

    /// <summary>
    /// A household device the caller may use at the role <paramref name="minimum"/> or higher (D6). A device of someone's own, of a
    /// household the caller is not in, or one that does not exist is "not found"; a role that is too low gets <paramref name="tooLow"/>.
    /// </summary>
    public static async Task<Result<Device>> FindUsableAsync(
        this IDeviceRepository devices, IHouseholdRoles households, Guid deviceId, HouseholdRole minimum, Error tooLow, CancellationToken cancellationToken)
    {
        var device = await devices.FindAsync(deviceId, cancellationToken);
        if (device?.HouseholdId is not { } householdId || device.IsDisabled)
        {
            return Result.Fail(DeviceErrors.NotFound);
        }

        return await households.GetRoleAsync(householdId, cancellationToken) switch
        {
            null => Result.Fail(DeviceErrors.NotFound),
            { } role when role < minimum => Result.Fail(tooLow),
            _ => Result.Ok(device),
        };
    }
}
