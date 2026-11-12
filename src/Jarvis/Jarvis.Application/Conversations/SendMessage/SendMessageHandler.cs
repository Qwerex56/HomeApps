using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;
using Jarvis.Application.Runs;
using Jarvis.Application.Runs.Ask;
using Jarvis.Domain.Conversations;

namespace Jarvis.Application.Conversations.SendMessage;

/// <summary>
/// The person's message is kept, JARVIS runs with the conversation so far as context (channel <c>app</c>, reference =
/// the conversation) and his answer is kept with its run, whose calls are the card.
/// </summary>
internal sealed class SendMessageHandler(
    OwnConversation own, IConversationRepository conversations, Agent agent, TimeProvider time)
    : ICommandHandler<SendMessageCommand, MessageView>
{
    public async Task<Result<MessageView>> Handle(SendMessageCommand command, CancellationToken cancellationToken)
    {
        var found = await own.FindAsync(command.ConversationId, command.UserId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult();
        }

        var conversation = found.Value;
        var history = (await conversations.MessagesAsync(conversation.Id, SendMessageCommand.History, cancellationToken))
            .Select(m => new ContextLine(m.Role == MessageRole.Person ? "Ty" : "JARVIS", m.Text, m.SentAt))
            .ToList();
        conversations.Add(conversation.Write(command.Text, time));

        var answer = await agent.RunAsync(
            new AskCommand(command.UserId, command.Text, command.HouseholdId, "app", conversation.Id.ToString(), history),
            command.StreamId,
            cancellationToken);

        var message = conversation.Answer(answer.Text, answer.RunId, time);
        conversations.Add(message);
        return MessageView.From(message, answer.Actions);
    }
}
