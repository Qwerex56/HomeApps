using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Mutes.Unmute;

internal sealed class UnmuteHandler(IRecipientRepository recipients, ICurrentUser currentUser) : ICommandHandler<UnmuteCommand>
{
    public async Task<Result> Handle(UnmuteCommand command, CancellationToken cancellationToken)
    {
        var recipient = await recipients.FindAsync(currentUser.RequiredUserId, cancellationToken);
        recipient?.Unmute(command.HouseholdId, command.Source);
        return Result.Ok();
    }
}
