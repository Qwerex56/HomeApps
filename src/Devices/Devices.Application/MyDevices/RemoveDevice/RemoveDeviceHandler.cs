using Devices.Application.Abstractions;
using Devices.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.MyDevices.RemoveDevice;

internal sealed class RemoveDeviceHandler(IDeviceRepository devices, IDeviceLinks links, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RemoveDeviceCommand>
{
    public async Task<Result> Handle(RemoveDeviceCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindMineAsync(command.DeviceId, currentUser.RequiredUserId, cancellationToken);
        if (device.IsFailed)
        {
            return device.ToResult();
        }

        device.Value.Disable(time.GetUtcNow());
        links.Drop(device.Value.Id);
        return Result.Ok();
    }
}
