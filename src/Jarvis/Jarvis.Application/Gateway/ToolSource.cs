namespace Jarvis.Application.Gateway;

/// <summary>An MCP server the gateway forwards to: a core service, HomeMix, or a plugin.</summary>
/// <param name="Prefix">Before each tool name, with <see cref="Separator"/>; empty for HomeMix.</param>
public sealed record ToolSource(string Name, string Prefix, Uri Endpoint)
{
    public const char Separator = '_';

    /// <summary>The name the gateway shows: <c>todo_add_task</c>.</summary>
    public string Expose(string tool) => Prefix.Length == 0 ? tool : $"{Prefix}{Separator}{tool}";

    /// <summary>The source's own name for <paramref name="exposed"/>, or <c>null</c> when it is not this source's.</summary>
    public string? Own(string exposed)
    {
        if (Prefix.Length == 0)
        {
            return exposed;
        }

        var start = Prefix + Separator;
        return exposed.StartsWith(start, StringComparison.Ordinal) && exposed.Length > start.Length ? exposed[start.Length..] : null;
    }

    /// <summary>A resource's URI as the gateway shows it: <c>{source}:{uri}</c>.</summary>
    public string ExposeUri(string uri) => $"{Name}:{uri}";
}
