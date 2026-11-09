using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;

namespace Notifications.Application.AccountsSync.ForgetDevices;

internal sealed class ForgetDevicesHandler(IRecipientRepository recipients) : ICommandHandler<ForgetDevicesCommand>
{
    public async Task<Result> Handle(ForgetDevicesCommand command, CancellationToken cancellationToken)
    {
        var recipient = await recipients.FindAsync(command.UserId, cancellationToken);
        recipient?.ForgetDevices();
        return Result.Ok();
    }
}
