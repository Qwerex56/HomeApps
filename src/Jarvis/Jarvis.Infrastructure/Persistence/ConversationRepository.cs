using Jarvis.Application.Abstractions;
using Jarvis.Domain.Conversations;
using Microsoft.EntityFrameworkCore;

namespace Jarvis.Infrastructure.Persistence;

internal sealed class ConversationRepository(JarvisDbContext db) : IConversationRepository
{
    public Task<Conversation?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Conversation>> ListForOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await db.Conversations.Where(c => c.OwnerId == ownerId).OrderByDescending(c => c.LastActivityAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ConversationMessage>> MessagesAsync(Guid conversationId, int count, CancellationToken cancellationToken)
    {
        var latest = await db.ConversationMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.Id)
            .Take(count)
            .ToListAsync(cancellationToken);
        latest.Reverse();
        return latest;
    }

    public void Add(Conversation conversation) => db.Conversations.Add(conversation);

    public void Add(ConversationMessage message) => db.ConversationMessages.Add(message);

    public void Remove(Conversation conversation) => db.Conversations.Remove(conversation);

    public Task ForgetAsync(Guid ownerId, CancellationToken cancellationToken) =>
        db.Conversations.Where(c => c.OwnerId == ownerId).ExecuteDeleteAsync(cancellationToken);
}
