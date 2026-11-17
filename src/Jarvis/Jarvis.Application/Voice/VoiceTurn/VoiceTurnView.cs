using Jarvis.Application.Common;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.Voice.VoiceTurn;

/// <param name="Answer"><c>null</c> when nothing was heard.</param>
public sealed record VoiceTurnView(Guid? ConversationId, string Transcript, MessageView? Answer)
{
    /// <summary>The actions now waiting for "tak" or "nie".</summary>
    public IReadOnlyList<Guid> Pending =>
        Answer?.Actions.Where(a => a.Status == ToolCallStatus.NeedsConfirmation).Select(a => a.Id).ToList() ?? [];
}
