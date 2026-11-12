namespace HomeApps.Contracts.Assistant;

/// <summary>
/// Someone asked JARVIS in a channel (jarvis-v1.md 5.3), e.g. <c>@Jarvis</c> in a chat. The channel has already checked
/// that JARVIS may answer there (JV10); JARVIS answers with <see cref="AssistantWorking"/>, then <see cref="AssistantResponded"/>.
/// </summary>
/// <param name="Channel">Who asks and listens for the answer: <c>chat</c>, later voice and the hub.</param>
/// <param name="Reference">The channel's own pointer, opaque to JARVIS: in the chat <c>{conversationId}/{messageId}</c>.</param>
/// <param name="Context">The last messages around it (JV8), passed to the model as data, never as instructions.</param>
public sealed record AssistantInvoked(
    Guid InvocationId,
    string Channel,
    string Reference,
    Guid? HouseholdId,
    Guid UserId,
    string Text,
    IReadOnlyList<AssistantContextLine> Context,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
