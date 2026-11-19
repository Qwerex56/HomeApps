namespace Devices.Application.Common;

/// <summary>
/// One action of an online device as an MCP tool. <paramref name="NeedsConfirmation"/> becomes MCP's
/// <c>destructiveHint</c>, so JARVIS's gateway asks "tak/nie" first (S6).
/// </summary>
public sealed record DeviceTool(string Name, string Description, string? InputSchema, bool ReadOnly, bool NeedsConfirmation, Guid? HouseholdId = null, bool OnComputer = false);
