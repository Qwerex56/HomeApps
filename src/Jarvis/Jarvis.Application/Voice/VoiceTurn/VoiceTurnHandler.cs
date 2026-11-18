using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Actions.Decide;
using Jarvis.Application.Common;
using Jarvis.Application.Conversations;
using Jarvis.Application.Conversations.SendMessage;
using Jarvis.Application.Conversations.StartConversation;
using Jarvis.Application.Runs;
using Jarvis.Application.Runs.Ask;
using Jarvis.Domain.Conversations;
using Jarvis.Domain.ToolCalls;
using MediatR;

namespace Jarvis.Application.Voice.VoiceTurn;

/// <summary>
/// Hears the turn (or takes the listener's own transcript), then either decides the actions waiting from the turn before ("tak" / "nie") or sends what was said
/// to the conversation as a message. The answer, its card and the decisions stay in the conversation as text; the
/// audio is never kept (V7). On a household satellite a turn with "PIN 1234" (or the one right after it) is the
/// person's: JARVIS answers as them, with their permissions, and nothing of it goes to the satellite's conversation.
/// </summary>
internal sealed class VoiceTurnHandler(
    ISpeechToText ears,
    ISender sender,
    OwnConversation own,
    IConversationRepository conversations,
    Agent agent,
    ISpeakers speakers,
    TimeProvider time) : ICommandHandler<VoiceTurnCommand, VoiceTurnView>
{
    public async Task<Result<VoiceTurnView>> Handle(VoiceTurnCommand command, CancellationToken cancellationToken)
    {
        var transcript = (command.Text ?? await ears.TranscribeAsync(command.Audio, command.SampleRate, cancellationToken)).Trim();
        if (transcript.Length == 0)
        {
            return new VoiceTurnView(command.ConversationId, transcript, null);
        }

        if (command is { Satellite: true, HouseholdId: { } household })
        {
            if (PinIntro.Of(transcript) is { } intro)
            {
                return await IntroducedAsync(command, household, intro, cancellationToken);
            }

            if (command.Speaker is { } speaker)
            {
                return await AsSpeakerAsync(command, household, speaker, transcript, transcript, cancellationToken);
            }
        }

        var conversationId = command.ConversationId;
        if (conversationId is null)
        {
            var started = await sender.Send(new StartConversationCommand(command.UserId, null, command.Satellite ? command.HouseholdId : null), cancellationToken);
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

    private async Task<Result<VoiceTurnView>> IntroducedAsync(
        VoiceTurnCommand command, Guid household, PinIntro intro, CancellationToken cancellationToken)
    {
        var match = await speakers.IdentifyAsync(command.UserId, household, intro.Name, intro.Pin, cancellationToken);
        if (match.Outcome != SpeakerOutcome.Known)
        {
            return Said(command, intro.Masked, match.Outcome switch
            {
                SpeakerOutcome.Ambiguous => "Ten PIN pasuje do kilku osób. Powiedz też, kto mówi.",
                SpeakerOutcome.Locked => "Za dużo błędnych PIN-ów. Spróbuj za kwadrans.",
                _ => "Nie rozpoznaję tego PIN-u.",
            }, [], null);
        }

        return intro.Request.Length == 0
            ? Said(command, intro.Masked, $"Słucham, {match.Name}.", [], match.UserId)
            : await AsSpeakerAsync(command, household, match.UserId, intro.Request, intro.Masked, cancellationToken);
    }

    /// <summary>The person's turn on the satellite: their run (channel <c>voice</c>, reference the device), their decisions.</summary>
    private async Task<Result<VoiceTurnView>> AsSpeakerAsync(
        VoiceTurnCommand command, Guid household, Guid speaker, string request, string transcript, CancellationToken cancellationToken)
    {
        if (command.Pending.Count > 0 && Consent.Of(request) is { } approve)
        {
            var decided = new List<ActionView>();
            foreach (var id in command.Pending)
            {
                var result = await sender.Send(new DecideCommand(speaker, id, approve), cancellationToken);
                if (result.IsSuccess)
                {
                    decided.Add(result.Value);
                }
            }

            return Said(command, transcript, Summary(decided, approve), decided, null);
        }

        var answer = await agent.RunAsync(
            new AskCommand(speaker, request, household, Prompts.VoiceChannel, $"device/{command.UserId}", []), null, cancellationToken);
        var waiting = answer.Actions.Any(a => a.Status == ToolCallStatus.NeedsConfirmation);
        return Said(command, transcript, answer.Text, answer.Actions, waiting ? speaker : null);
    }

    /// <summary>An answer for the satellite only, not kept in its conversation.</summary>
    private VoiceTurnView Said(VoiceTurnCommand command, string transcript, string text, IReadOnlyList<ActionView> actions, Guid? speaker) =>
        new(command.ConversationId, transcript, new MessageView(Guid.NewGuid(), MessageRole.Assistant, text, time.GetUtcNow(), actions), speaker);

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
