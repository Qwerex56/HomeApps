using System.Text.Json;
using HomeMix.Domain.Tools;

namespace HomeMix.Api.Mcp;

/// <summary>A resolved call: where, which tool (or the summary when <see cref="Tool"/> is null) and its checked arguments.</summary>
/// <param name="Fields">The document fields from the arguments (defaults filled in for add).</param>
public sealed record ToolCall(ToolPlace Place, AddonTool? Tool, string? Key, IReadOnlyDictionary<string, JsonElement> Fields);
