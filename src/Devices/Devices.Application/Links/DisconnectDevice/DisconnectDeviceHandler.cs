using Devices.Application.Abstractions;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Links.DisconnectDevice;

internal sealed class DisconnectDeviceHandler(IDeviceRepository devices, IDeviceStream stream, TimeProvider time) : ICommandHandler<DisconnectDeviceCommand>
{
    public async Task<Result> Handle(DisconnectDeviceCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindAsync(command.DeviceId, cancellationToken);
        device?.Seen(time.GetUtcNow());
        if (device?.HouseholdId is { } householdId)
        {
            stream.Publish(new DeviceStreamMessage(householdId, "online", new { deviceId = device.Id, online = false }));
        }

        return Result.Ok();
    }
}
