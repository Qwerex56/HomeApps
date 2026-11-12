using Jarvis.Domain.Conversations;

namespace Jarvis.Application.Common;

public sealed record ConversationView(Guid Id, string Title, DateTimeOffset LastActivityAt)
{
    public static ConversationView From(Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        return new ConversationView(conversation.Id, conversation.Title, conversation.LastActivityAt);
    }
}
