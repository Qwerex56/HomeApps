using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.ToolCalls.RecordCall;

/// <summary>The gateway called a tool in a run of <paramref name="UserId"/>'s; it goes to the run's log and card.</summary>
public sealed record RecordCallCommand(
    Guid RunId, Guid UserId, string Tool, string? Arguments, string Result, bool Failed, bool ReadOnly) : ICommand;
