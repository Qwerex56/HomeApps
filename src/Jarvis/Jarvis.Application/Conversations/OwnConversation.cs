using FluentResults;
using Jarvis.Application.Abstractions;
using Jarvis.Domain.Conversations;

namespace Jarvis.Application.Conversations;

/// <summary>The conversation, if it is the person's; someone else's does not exist for them.</summary>
internal sealed class OwnConversation(IConversationRepository conversations)
{
    public async Task<Result<Conversation>> FindAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        await conversations.FindAsync(id, cancellationToken) is { } conversation && conversation.IsOwnedBy(userId)
            ? conversation
            : Result.Fail(ConversationErrors.NotFound);
}
