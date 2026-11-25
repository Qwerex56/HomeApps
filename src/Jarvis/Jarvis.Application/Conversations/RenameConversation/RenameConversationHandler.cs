using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.Conversations.RenameConversation;

internal sealed class RenameConversationHandler(OwnConversation own) : ICommandHandler<RenameConversationCommand>
{
    public async Task<Result> Handle(RenameConversationCommand command, CancellationToken cancellationToken)
    {
        var found = await own.FindAsync(command.ConversationId, command.UserId, cancellationToken);
        return found.IsFailed ? found.ToResult() : found.Value.Rename(command.Title);
    }
}
