using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;

namespace Jarvis.Application.Conversations.ReadConversation;

/// <summary>The messages with each answer's action card, as it stands now (waiting ones may have expired).</summary>
internal sealed class ReadConversationHandler(
    OwnConversation own, IConversationRepository conversations, IToolCallRepository calls, TimeProvider time)
    : IQueryHandler<ReadConversationQuery, IReadOnlyList<MessageView>>
{
    public async Task<Result<IReadOnlyList<MessageView>>> Handle(ReadConversationQuery query, CancellationToken cancellationToken)
    {
        var conversation = await own.FindAsync(query.ConversationId, query.UserId, cancellationToken);
        if (conversation.IsFailed)
        {
            return conversation.ToResult();
        }

        var messages = await conversations.MessagesAsync(conversation.Value.Id, ReadConversationQuery.Limit, cancellationToken);
        var runIds = messages.Where(m => m.RunId.HasValue).Select(m => m.RunId!.Value).ToList();
        var now = time.GetUtcNow();
        var cards = (await calls.ListForRunsAsync(runIds, cancellationToken))
            .Where(c => !c.ReadOnly)
            .GroupBy(c => c.RunId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ActionView>)g.Select(c => ActionView.From(c, now)).ToList());
        return messages
            .Select(m => MessageView.From(m, m.RunId is { } run && cards.TryGetValue(run, out var card) ? card : []))
            .ToList();
    }
}
