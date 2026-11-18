using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Capabilities.SetCapabilityHidden;

internal sealed class SetCapabilityHiddenHandler(ICapabilityRegistry registry, ICurrentUser currentUser)
    : ICommandHandler<SetCapabilityHiddenCommand>
{
    public async Task<Result> Handle(SetCapabilityHiddenCommand command, CancellationToken cancellationToken)
    {
        var owner = currentUser.RequireOwner();
        if (owner.IsFailed)
        {
            return owner;
        }

        if (await registry.FindAsync(command.Name, cancellationToken) is not { } capability)
        {
            return Result.Fail(DeviceErrors.CapabilityNotFound);
        }

        if (command.Hidden)
        {
            capability.Hide();
        }
        else
        {
            capability.Unhide();
        }

        return Result.Ok();
    }
}
