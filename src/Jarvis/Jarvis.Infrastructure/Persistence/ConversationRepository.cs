using Jarvis.Application.Abstractions;
using Jarvis.Domain.Conversations;
using Microsoft.EntityFrameworkCore;

namespace Jarvis.Infrastructure.Persistence;

internal sealed class ConversationRepository(JarvisDbContext db) : IConversationRepository
{
    public Task<Conversation?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Conversation>> ListForOwnerAsync(Guid ownerId, string? search, CancellationToken cancellationToken)
    {
        var owned = db.Conversations.Where(c => c.OwnerId == ownerId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            // 2.14: % _ and the escape character itself are literal in the pattern.
            var pattern = "%" + search.Trim().Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal) + "%";
            owned = owned.Where(c => EF.Functions.ILike(c.Title, pattern, "\\")
                || db.ConversationMessages.Any(m => m.ConversationId == c.Id && EF.Functions.ILike(m.Text, pattern, "\\")));
        }

        return await owned.OrderByDescending(c => c.LastActivityAt).ToListAsync(cancellationToken);
    }

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

    public Task ForgetSatelliteMessagesAsync(DateTimeOffset before, CancellationToken cancellationToken) =>
        db.ConversationMessages
            .Where(m => m.SentAt < before && db.Conversations.Any(c => c.Id == m.ConversationId && c.HouseholdId != null))
            .ExecuteDeleteAsync(cancellationToken);
}
