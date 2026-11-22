using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.HouseholdDevices.SetJarvisAccess;

internal sealed class SetJarvisAccessHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceStream stream, ICurrentUser currentUser)
    : ICommandHandler<SetJarvisAccessCommand>
{
    public async Task<Result> Handle(SetJarvisAccessCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindManageableAsync(households, command.DeviceId, currentUser.RequiredUserId, cancellationToken);
        if (device.IsFailed)
        {
            return device.ToResult();
        }

        // Only a household device has the switches: a person's own computer is not under a household's rules.
        if (device.Value.HouseholdId is not { } householdId)
        {
            return Result.Fail(DeviceErrors.NotFound);
        }

        device.Value.SetJarvisAccess(command.MayControl, command.Asks);
        stream.Publish(new DeviceStreamMessage(householdId, "changed", new { deviceId = device.Value.Id, reason = "jarvis" }));
        return Result.Ok();
    }
}
