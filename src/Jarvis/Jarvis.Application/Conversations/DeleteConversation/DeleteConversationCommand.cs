using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.Conversations.DeleteConversation;

/// <summary>Gone with its messages; the runs stay in the log for their retention (jarvis-v1.md 2.5).</summary>
public sealed record DeleteConversationCommand(Guid UserId, Guid ConversationId) : ICommand;
