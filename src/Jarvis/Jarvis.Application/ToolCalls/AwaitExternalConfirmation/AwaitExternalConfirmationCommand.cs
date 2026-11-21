using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.ToolCalls.AwaitExternalConfirmation;

/// <summary>
/// An action on a person's computer called by an external MCP client that asks for confirmation:
/// creates a closed run (channel "mcp", reference = clientId) and a ToolCall in NeedsConfirmation,
/// or returns the existing pending action if identical. Emits NotificationRequested to the person.
/// </summary>
public sealed record AwaitExternalConfirmationCommand(
    Guid UserId,
    string ClientId,
    string Tool,
    string? Arguments,
    string? DeviceName) : ICommand<Guid>;
