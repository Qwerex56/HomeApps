using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;

namespace Jarvis.Api.Tests;

/// <summary>
/// The tools of a fake source that a test chooses (empty by default, so other tests never see it) and the calls it got. The
/// source lists the set on every request, so a test can change a tool's annotations between two calls.
/// </summary>
internal sealed class FakeToolSet
{
    private static readonly JsonElement ObjectSchema = JsonElement.Parse("""{"type":"object","properties":{}}""");

    public List<Tool> Tools { get; set; } = [];

    /// <summary>Names of the tools that were really called on the source.</summary>
    public ConcurrentQueue<string> Calls { get; } = new();

    /// <summary>How many times the source was asked for its tool list.</summary>
    public int Listed { get; private set; }

    public void Reset()
    {
        Tools = [];
        Calls.Clear();
        Listed = 0;
    }

    public ListToolsResult List()
    {
        Listed++;
        return new ListToolsResult { Tools = [.. Tools] };
    }

    public CallToolResult Call(string name)
    {
        Calls.Enqueue(name);
        return new CallToolResult { Content = [new TextContentBlock { Text = $"{name} done" }] };
    }

    /// <summary>A tool of a person's computer: <c>_meta</c> purpose "computer".</summary>
    public static Tool Computer(string name, bool? readOnly = null, bool? destructive = null, bool withAnnotations = true) => Make(
        name, withAnnotations ? new ToolAnnotations { ReadOnlyHint = readOnly, DestructiveHint = destructive } : null,
        new JsonObject { ["homeapps/purpose"] = "computer" });

    /// <summary>A tool of a household device: <c>_meta</c> names the household.</summary>
    public static Tool Household(string name, Guid household, bool? readOnly = null, bool? destructive = null) => Make(
        name, new ToolAnnotations { ReadOnlyHint = readOnly, DestructiveHint = destructive },
        new JsonObject { ["homeapps/household"] = household.ToString() });

    public static Tool Plain(string name, bool? readOnly = null, bool? destructive = null) => Make(
        name, new ToolAnnotations { ReadOnlyHint = readOnly, DestructiveHint = destructive }, null);

    private static Tool Make(string name, ToolAnnotations? annotations, JsonObject? meta) =>
        new() { Name = name, Description = name, InputSchema = ObjectSchema, Annotations = annotations, Meta = meta };
}
