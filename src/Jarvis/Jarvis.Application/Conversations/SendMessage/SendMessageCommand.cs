using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Common;

namespace Jarvis.Application.Conversations.SendMessage;

/// <summary>The person writes in their conversation and JARVIS answers; the answer's pieces go to <paramref name="StreamId"/>.</summary>
/// <param name="HouseholdId">The household picked in the app, JARVIS's default (jarvis-v1.md 2.4).</param>
public sealed record SendMessageCommand(Guid UserId, Guid ConversationId, string Text, Guid? HouseholdId, Guid? StreamId)
    : ICommand<MessageView>
{
    /// <summary>The earlier messages JARVIS sees, so he follows the conversation.</summary>
    public const int History = 20;
}
