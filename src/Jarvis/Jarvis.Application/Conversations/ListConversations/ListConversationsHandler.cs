using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;

namespace Jarvis.Application.Conversations.ListConversations;

internal sealed class ListConversationsHandler(IConversationRepository conversations)
    : IQueryHandler<ListConversationsQuery, IReadOnlyList<ConversationView>>
{
    public async Task<Result<IReadOnlyList<ConversationView>>> Handle(ListConversationsQuery query, CancellationToken cancellationToken) =>
        (await conversations.ListForOwnerAsync(query.UserId, cancellationToken)).Select(ConversationView.From).ToList();
}
