using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.HouseholdDevices.RenameDevice;

internal sealed class RenameDeviceHandler(IDeviceRepository devices, IHouseholdRoles households, ICurrentUser currentUser)
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
        return sameName is not null && sameName.Id != device.Id ? Result.Fail(DeviceErrors.NameTaken) : device.Rename(name.Value);
    }
}
