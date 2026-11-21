using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.HouseholdDevices.RenameDevice;

internal sealed class RenameDeviceHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceStream stream, ICurrentUser currentUser)
    : ICommandHandler<RenameDeviceCommand>
{
    public async Task<Result> Handle(RenameDeviceCommand command, CancellationToken cancellationToken)
    {
        var found = await devices.FindManageableAsync(households, command.DeviceId, currentUser.RequiredUserId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult();
        }

        var device = found.Value;
        var name = DeviceName.Normalize(command.Name);
        if (name.IsFailed)
        {
            return name.ToResult();
        }

        var sameName = device.HouseholdId is { } householdId
            ? await devices.FindInHouseholdAsync(householdId, name.Value, cancellationToken)
            : await devices.FindByNameAsync(device.OwnerId, name.Value, cancellationToken);
        if (sameName is not null && sameName.Id != device.Id)
        {
            return Result.Fail(DeviceErrors.NameTaken);
        }

        var renamed = device.Rename(name.Value);
        if (renamed.IsSuccess && device.HouseholdId is { } hid)
        {
            stream.Publish(new DeviceStreamMessage(hid, "changed", new { deviceId = device.Id, reason = "renamed" }));
        }

        return renamed;
    }
}
