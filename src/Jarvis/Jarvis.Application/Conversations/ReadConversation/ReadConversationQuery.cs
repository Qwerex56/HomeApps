using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Common;

namespace Jarvis.Application.Conversations.ReadConversation;

public sealed record ReadConversationQuery(Guid UserId, Guid ConversationId) : IQuery<IReadOnlyList<MessageView>>
{
    /// <summary>Enough for the page; older messages wait for paging when somebody needs it.</summary>
    public const int Limit = 200;
}
