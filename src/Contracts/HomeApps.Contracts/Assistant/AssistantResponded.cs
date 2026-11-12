namespace HomeApps.Contracts.Assistant;

/// <summary>
/// JARVIS's answer to <see cref="AssistantInvoked"/>, or the new state of an action after <see cref="AssistantConfirmed"/>
/// (then <see cref="Text"/> says what happened). A channel takes only those with its own <see cref="Channel"/>.
/// </summary>
/// <param name="Text">Plain text, Polish.</param>
public sealed record AssistantResponded(
    Guid InvocationId,
    string Channel,
    string Reference,
    string Text,
    IReadOnlyList<AssistantAction> Actions) : IIntegrationEvent;
