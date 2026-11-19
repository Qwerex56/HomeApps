using Jarvis.Application.Common;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.Voice.VoiceTurn;

/// <param name="Answer"><c>null</c> when nothing was heard.</param>
/// <param name="Speaker">On a satellite, the person this turn was for: the next turns are theirs for a few minutes.</param>
/// <param name="Listening">After "tu Adrian, PIN 1234" alone: the person goes on without the wake word.</param>
public sealed record VoiceTurnView(Guid? ConversationId, string Transcript, MessageView? Answer, Guid? Speaker = null, bool Listening = false)
{
    /// <summary>The actions now waiting for "tak" or "nie".</summary>
    public IReadOnlyList<Guid> Pending =>
        Answer?.Actions.Where(a => a.Status == ToolCallStatus.NeedsConfirmation).Select(a => a.Id).ToList() ?? [];
}
