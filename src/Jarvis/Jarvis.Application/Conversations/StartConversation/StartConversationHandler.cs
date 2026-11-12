using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;
using Jarvis.Domain.Conversations;

namespace Jarvis.Application.Conversations.StartConversation;

internal sealed class StartConversationHandler(IConversationRepository conversations, TimeProvider time)
    : ICommandHandler<StartConversationCommand, ConversationView>
{
    public Task<Result<ConversationView>> Handle(StartConversationCommand command, CancellationToken cancellationToken)
    {
        var conversation = Conversation.Start(command.UserId, command.Title, time);
        conversations.Add(conversation);
        return Task.FromResult(Result.Ok(ConversationView.From(conversation)));
    }
}
