using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;

namespace Jarvis.Application.Conversations.DeleteConversation;

internal sealed class DeleteConversationHandler(OwnConversation own, IConversationRepository conversations)
    : ICommandHandler<DeleteConversationCommand>
{
    public async Task<Result> Handle(DeleteConversationCommand command, CancellationToken cancellationToken)
    {
        var found = await own.FindAsync(command.ConversationId, command.UserId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult();
        }

        conversations.Remove(found.Value);
        return Result.Ok();
    }
}
