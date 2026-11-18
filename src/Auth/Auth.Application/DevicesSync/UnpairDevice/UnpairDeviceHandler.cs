using Auth.Application.Abstractions;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.DevicesSync.UnpairDevice;

internal sealed class UnpairDeviceHandler(IDeviceIdentityRepository devices, TimeProvider time) : ICommandHandler<UnpairDeviceCommand>
{
    public async Task<Result> Handle(UnpairDeviceCommand command, CancellationToken cancellationToken)
    {
        (await devices.GetByIdAsync(command.DeviceId, cancellationToken))?.Unpair(time.GetUtcNow());
        return Result.Ok();
    }
}
