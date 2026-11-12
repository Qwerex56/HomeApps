using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Common;

namespace Jarvis.Application.Conversations.StartConversation;

/// <param name="Title">Optional; without it the first message names the conversation.</param>
public sealed record StartConversationCommand(Guid UserId, string? Title) : ICommand<ConversationView>;
