using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Devices.ForgetDevice;

internal sealed class ForgetDeviceHandler(IRecipientRepository recipients, ICurrentUser currentUser)
    : ICommandHandler<ForgetDeviceCommand>
{
    public async Task<Result> Handle(ForgetDeviceCommand command, CancellationToken cancellationToken)
    {
        var recipient = await recipients.FindAsync(currentUser.RequiredUserId, cancellationToken);
        recipient?.ForgetEndpoint(command.Endpoint);
        return Result.Ok();
    }
}
