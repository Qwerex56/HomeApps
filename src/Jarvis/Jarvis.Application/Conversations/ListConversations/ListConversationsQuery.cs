using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Common;

namespace Jarvis.Application.Conversations.ListConversations;

public sealed record ListConversationsQuery(Guid UserId) : IQuery<IReadOnlyList<ConversationView>>;
