namespace Jarvis.Application.Gateway;

/// <param name="Prefix">Put before its tool names with <c>_</c>; <c>null</c> = <see cref="Name"/>, empty = none.</param>
public sealed class SourceOptions
{
    public string Name { get; set; } = string.Empty;

    /// <summary>The MCP endpoint inside the Docker network, e.g. <c>http://households:8080/mcp</c>.</summary>
    public string Url { get; set; } = string.Empty;

    public string? Prefix { get; set; }
}
