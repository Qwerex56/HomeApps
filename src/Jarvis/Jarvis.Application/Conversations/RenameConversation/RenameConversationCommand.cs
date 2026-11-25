using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.Conversations.RenameConversation;

/// <summary>2.14: the person names their conversation (1–80 characters). Someone else's conversation does not exist for them: 404.</summary>
public sealed record RenameConversationCommand(Guid UserId, Guid ConversationId, string? Title) : ICommand;
