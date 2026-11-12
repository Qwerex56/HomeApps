namespace HomeApps.Contracts.Assistant;

/// <summary>JARVIS took <see cref="AssistantInvoked"/> and thinks: the chat shows "JARVIS pisze…".</summary>
public sealed record AssistantWorking(Guid InvocationId, string Channel, string Reference) : IIntegrationEvent;
