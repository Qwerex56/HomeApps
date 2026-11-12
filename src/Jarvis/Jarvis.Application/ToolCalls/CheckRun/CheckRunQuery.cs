using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.ToolCalls.CheckRun;

/// <summary>Before the gateway calls a tool in a run: is it the caller's, and still going? Answers the run's id.</summary>
public sealed record CheckRunQuery(Guid RunId, Guid UserId) : IQuery<Guid>;
