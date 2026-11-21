using Devices.Application.Abstractions;
using Devices.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Devices;

namespace Devices.Application.MyDevices.RemoveDevice;

internal sealed class RemoveDeviceHandler(
    IDeviceRepository devices, IHouseholdRoles households, IDeviceLinks links, IDeviceStream stream, IEventBus bus, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RemoveDeviceCommand>
{
    public async Task<Result> Handle(RemoveDeviceCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindManageableAsync(households, command.DeviceId, currentUser.RequiredUserId, cancellationToken);
        if (device.IsFailed)
        {
            return device.ToResult();
        }

        var satellite = device.Value.IsPairedSatellite;
        device.Value.Disable(time.GetUtcNow());
        links.Drop(device.Value.Id);
        if (satellite)
        {
            // S13: it leaves the household and its secret stops working.
            await bus.PublishAsync(new HouseholdDeviceUnpaired(device.Value.Id, device.Value.HouseholdId!.Value), cancellationToken);
        }

        if (device.Value.HouseholdId is { } householdId)
        {
            stream.Publish(new DeviceStreamMessage(householdId, "changed", new { deviceId = device.Value.Id, reason = "removed" }));
        }

        return Result.Ok();
    }
}
