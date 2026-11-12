namespace Jarvis.Domain.Conversations;

/// <summary>One message of a <see cref="Conversation"/>; JARVIS's points to its run, whose tool calls are its action card.</summary>
public sealed class ConversationMessage
{
    private ConversationMessage()
    {
    }

    public Guid Id { get; private set; }

    public Guid ConversationId { get; private set; }

    public MessageRole Role { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public Guid? RunId { get; private set; }

    public DateTimeOffset SentAt { get; private set; }

    internal static ConversationMessage Create(Guid conversationId, MessageRole role, string text, Guid? runId, TimeProvider time)
    {
        var now = time.GetUtcNow();
        return new ConversationMessage
        {
            Id = Guid.CreateVersion7(now),
            ConversationId = conversationId,
            Role = role,
            Text = text,
            RunId = runId,
            SentAt = now,
        };
    }
}
