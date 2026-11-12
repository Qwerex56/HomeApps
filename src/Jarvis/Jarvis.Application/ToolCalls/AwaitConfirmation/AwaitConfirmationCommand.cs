using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.ToolCalls.AwaitConfirmation;

/// <summary>A destructive tool in a run: kept, not called, until the person confirms (JV9). Returns the action's id.</summary>
public sealed record AwaitConfirmationCommand(Guid RunId, Guid UserId, string Tool, string? Arguments) : ICommand<Guid>;
