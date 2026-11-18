using Devices.Application.Abstractions;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.AccountsSync.EraseOwner;

internal sealed class EraseOwnerHandler(IDeviceRepository devices, IDeviceLinks links) : ICommandHandler<EraseOwnerCommand>
{
    public async Task<Result> Handle(EraseOwnerCommand command, CancellationToken cancellationToken)
    {
        foreach (var device in await devices.OfOwnerAsync(command.UserId, cancellationToken))
        {
            links.Drop(device.Id);
            devices.Remove(device);
        }

        return Result.Ok();
    }
}
