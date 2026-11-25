using Jarvis.Domain.Conversations;

namespace Jarvis.Application.Abstractions;

public interface IConversationRepository
{
    Task<Conversation?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The person's conversations, latest activity first.</summary>
    /// <summary>The owner's conversations, newest activity first; <paramref name="search"/> keeps those whose title or any message contains it (case-insensitive).</summary>
    Task<IReadOnlyList<Conversation>> ListForOwnerAsync(Guid ownerId, string? search, CancellationToken cancellationToken);

    /// <summary>The last <paramref name="count"/> messages, oldest first.</summary>
    Task<IReadOnlyList<ConversationMessage>> MessagesAsync(Guid conversationId, int count, CancellationToken cancellationToken);

    void Add(Conversation conversation);

    void Add(ConversationMessage message);

    /// <summary>With its messages.</summary>
    void Remove(Conversation conversation);

    Task ForgetAsync(Guid ownerId, CancellationToken cancellationToken);

    /// <summary>Household satellites' messages sent before <paramref name="before"/> go (a person's own stay).</summary>
    Task ForgetSatelliteMessagesAsync(DateTimeOffset before, CancellationToken cancellationToken);
}
