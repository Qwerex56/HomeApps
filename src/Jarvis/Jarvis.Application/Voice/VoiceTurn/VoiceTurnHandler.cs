using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Actions.Decide;
using Jarvis.Application.Common;
using Jarvis.Application.Conversations;
using Jarvis.Application.Conversations.SendMessage;
using Jarvis.Application.Conversations.StartConversation;
using Jarvis.Domain.ToolCalls;
using MediatR;

namespace Jarvis.Application.Voice.VoiceTurn;

/// <summary>
/// Hears the turn (or takes the listener's own transcript), then either decides the actions waiting from the turn before ("tak" / "nie") or sends what was said
/// to the conversation as a message. The answer, its card and the decisions stay in the conversation as text; the
/// audio is never kept (V7).
/// </summary>
internal sealed class VoiceTurnHandler(
    ISpeechToText ears,
    ISender sender,
    OwnConversation own,
    IConversationRepository conversations,
    TimeProvider time) : ICommandHandler<VoiceTurnCommand, VoiceTurnView>
{
    public async Task<Result<VoiceTurnView>> Handle(VoiceTurnCommand command, CancellationToken cancellationToken)
    {
        var transcript = (command.Text ?? await ears.TranscribeAsync(command.Audio, command.SampleRate, cancellationToken)).Trim();
        if (transcript.Length == 0)
        {
            return new VoiceTurnView(command.ConversationId, transcript, null);
        }

        var conversationId = command.ConversationId;
        if (conversationId is null)
        {
            var started = await sender.Send(new StartConversationCommand(command.UserId, null), cancellationToken);
            if (started.IsFailed)
            {
                return started.ToResult();
            }

            conversationId = started.Value.Id;
        }

        if (command.Pending.Count > 0 && Consent.Of(transcript) is { } approve)
        {
            var decided = await DecideAsync(command, conversationId.Value, transcript, approve, cancellationToken);
            return decided.IsFailed ? decided.ToResult() : new VoiceTurnView(conversationId, transcript, decided.Value);
        }

        var answer = await sender.Send(
            new SendMessageCommand(command.UserId, conversationId.Value, transcript, command.HouseholdId, null, Spoken: true),
            cancellationToken);
        return answer.IsFailed ? answer.ToResult() : new VoiceTurnView(conversationId, transcript, answer.Value);
    }

    private async Task<Result<MessageView>> DecideAsync(
        VoiceTurnCommand command, Guid conversationId, string transcript, bool approve, CancellationToken cancellationToken)
    {
        var found = await own.FindAsync(conversationId, command.UserId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult();
        }

        var decided = new List<ActionView>();
        foreach (var id in command.Pending)
        {
            var result = await sender.Send(new DecideCommand(command.UserId, id, approve), cancellationToken);
            if (result.IsSuccess)
            {
                decided.Add(result.Value);
            }
        }

        var conversation = found.Value;
        conversations.Add(conversation.Write(transcript, time));
        // No run of its own: the card of the answer that asked shows how the actions ended.
        var message = conversation.Answer(Summary(decided, approve), null, time);
        conversations.Add(message);
        return MessageView.From(message, decided);
    }

    private static string Summary(List<ActionView> decided, bool approve)
    {
        if (decided.Count == 0)
        {
            return "Na to już za późno, potwierdzenie wygasło. Poproś jeszcze raz.";
        }

        if (!approve)
        {
            return "Dobrze, nic nie zmieniam.";
        }

        return string.Join(" ", decided.Select(a => a.Status == ToolCallStatus.Done
            ? $"Zrobione: {a.Description}"
            : $"Nie udało się: {a.Description}"));
    }
}
