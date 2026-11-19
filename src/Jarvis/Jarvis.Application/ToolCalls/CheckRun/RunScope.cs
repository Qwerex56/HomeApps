namespace Jarvis.Application.ToolCalls.CheckRun;

/// <summary>
/// A run the gateway may log into, the household it works in (the household ceiling, H-2), and whether a household
/// satellite asked (then a person's computer tools are out of reach, R1).
/// </summary>
public sealed record RunScope(Guid Id, Guid? HouseholdId, bool Satellite);
