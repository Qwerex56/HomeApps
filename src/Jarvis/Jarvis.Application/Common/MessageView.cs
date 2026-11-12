using Jarvis.Domain.Conversations;

namespace Jarvis.Application.Common;

/// <param name="Actions">JARVIS's action card; empty for the person's messages.</param>
public sealed record MessageView(Guid Id, MessageRole Role, string Text, DateTimeOffset SentAt, IReadOnlyList<ActionView> Actions)
{
    public static MessageView From(ConversationMessage message, IReadOnlyList<ActionView> actions)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new MessageView(message.Id, message.Role, message.Text, message.SentAt, actions);
    }
}
