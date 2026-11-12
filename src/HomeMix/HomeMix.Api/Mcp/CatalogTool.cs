using System.Text.Json;

namespace HomeMix.Api.Mcp;

/// <summary>A tool as MCP lists it for this caller: name with the add-on's id, input schema and hints for JARVIS.</summary>
/// <param name="ReadOnly">list, get and the summary: JARVIS calls them without asking.</param>
/// <param name="Destructive">delete: JARVIS asks the person first. add and update are neither.</param>
public sealed record CatalogTool(string Name, string Title, string Description, JsonElement InputSchema, bool ReadOnly, bool Destructive);
