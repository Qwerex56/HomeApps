using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Common;

namespace Jarvis.Application.Conversations.StartConversation;

/// <param name="Title">Optional; without it the first message names the conversation.</param>
/// <param name="HouseholdId">Only for a household satellite's conversation (its messages last a week).</param>
public sealed record StartConversationCommand(Guid UserId, string? Title, Guid? HouseholdId = null) : ICommand<ConversationView>;
