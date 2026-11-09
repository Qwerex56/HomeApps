using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;

namespace Notifications.Application.AccountsSync.EraseRecipient;

internal sealed class EraseRecipientHandler(IRecipientRepository recipients) : ICommandHandler<EraseRecipientCommand>
{
    public async Task<Result> Handle(EraseRecipientCommand command, CancellationToken cancellationToken)
    {
        var recipient = await recipients.FindAsync(command.UserId, cancellationToken);
        if (recipient is not null)
        {
            recipients.Remove(recipient);
        }

        return Result.Ok();
    }
}
