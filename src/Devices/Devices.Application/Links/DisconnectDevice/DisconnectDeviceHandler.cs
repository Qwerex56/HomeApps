using Devices.Application.Abstractions;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Links.DisconnectDevice;

internal sealed class DisconnectDeviceHandler(IDeviceRepository devices, TimeProvider time) : ICommandHandler<DisconnectDeviceCommand>
{
    public async Task<Result> Handle(DisconnectDeviceCommand command, CancellationToken cancellationToken)
    {
        (await devices.FindAsync(command.DeviceId, cancellationToken))?.Seen(time.GetUtcNow());
        return Result.Ok();
    }
}
