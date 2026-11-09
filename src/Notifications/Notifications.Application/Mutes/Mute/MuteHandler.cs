using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Mutes.Mute;

internal sealed class MuteHandler(IRecipientRepository recipients, ICurrentUser currentUser) : ICommandHandler<MuteCommand>
{
    public async Task<Result> Handle(MuteCommand command, CancellationToken cancellationToken)
    {
        var recipient = await recipients.GetOrCreateAsync(currentUser.RequiredUserId, cancellationToken);
        return recipient.Mute(command.HouseholdId, command.Source);
    }
}
