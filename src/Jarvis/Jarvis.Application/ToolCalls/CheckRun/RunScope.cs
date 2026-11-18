namespace Jarvis.Application.ToolCalls.CheckRun;

/// <summary>A run the gateway may log into, and the household it works in (the household ceiling, H-2).</summary>
public sealed record RunScope(Guid Id, Guid? HouseholdId);
