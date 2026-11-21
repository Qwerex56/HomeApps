using HomeApps.BuildingBlocks.Application.Messaging;

namespace Jarvis.Application.Actions.GetActionResult;

/// <summary>
/// Returns the outcome of an action for external MCP clients or read-only checks:
/// pending / approved + text / denied / expired. Only the owner sees it.
/// </summary>
public sealed record GetActionResultQuery(Guid UserId, Guid ActionId) : IQuery<ActionResultView>;
