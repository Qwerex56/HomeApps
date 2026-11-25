using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Common;

namespace Jarvis.Application.Conversations.ListConversations;

/// <summary>The person's conversations; <paramref name="Search"/> (2.14, at most 100 characters) filters by title and message text.</summary>
public sealed record ListConversationsQuery(Guid UserId, string? Search = null) : IQuery<IReadOnlyList<ConversationView>>;
