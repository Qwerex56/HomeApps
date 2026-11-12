using Jarvis.Domain.Conversations;

namespace Jarvis.Application.Abstractions;

public interface IConversationRepository
{
    Task<Conversation?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The person's conversations, latest activity first.</summary>
    Task<IReadOnlyList<Conversation>> ListForOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    /// <summary>The last <paramref name="count"/> messages, oldest first.</summary>
    Task<IReadOnlyList<ConversationMessage>> MessagesAsync(Guid conversationId, int count, CancellationToken cancellationToken);

    void Add(Conversation conversation);

    void Add(ConversationMessage message);

    /// <summary>With its messages.</summary>
    void Remove(Conversation conversation);

    Task ForgetAsync(Guid ownerId, CancellationToken cancellationToken);
}
