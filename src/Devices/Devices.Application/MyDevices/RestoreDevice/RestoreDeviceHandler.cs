using Devices.Application.Abstractions;
using Devices.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.MyDevices.RestoreDevice;

internal sealed class RestoreDeviceHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceStream stream, ICurrentUser currentUser) : ICommandHandler<RestoreDeviceCommand>
{
    public async Task<Result> Handle(RestoreDeviceCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindManageableAsync(households, command.DeviceId, currentUser.RequiredUserId, cancellationToken);
        if (device.IsFailed)
        {
            return device.ToResult();
        }

        device.Value.Enable();
        if (device.Value.HouseholdId is { } householdId)
        {
            stream.Publish(new DeviceStreamMessage(householdId, "changed", new { deviceId = device.Value.Id, reason = "restored" }));
        }

        return Result.Ok();
    }
}
