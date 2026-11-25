using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Jarvis.Domain.Conversations;

public static class ConversationErrors
{
    public static Error TitleInvalid => new AppError(
        "jarvis.conversation_title_invalid", $"The title needs 1–{Conversation.TitleLength} characters.", ErrorKind.Validation);

    /// <summary>Also for someone else's: a conversation is only its owner's.</summary>
    public static Error NotFound => new AppError("jarvis.conversation_not_found", "There is no such conversation.", ErrorKind.NotFound);
}
