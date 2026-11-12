using HomeMix.Domain.Data;

namespace HomeMix.Domain.Tools;

/// <summary>
/// A tool declared in <c>homemix.json</c>, served over MCP as <c>{addonId}_{Name}</c>: an operation on one collection of
/// the add-on's data, or a script <c>tools/{Name}.js</c> run in Jint with the bridge's <c>ctx</c>.
/// </summary>
/// <param name="Permission">An add-on permission the person's household role must have to see and call it.</param>
/// <param name="Answer">The text it answers, with <c>{field}</c> and <c>{key}</c> filled in.</param>
/// <param name="ReadOnly">A script that only reads: it may not write data, and JARVIS calls it without asking.</param>
public sealed record AddonTool(
    string Name,
    string Description,
    string Collection,
    ToolAction Action,
    DataScope Scope,
    IReadOnlyList<AddonToolField> Fields,
    string? Permission,
    string? Answer,
    bool ReadOnly = false)
{
    public const string ScriptFolder = "tools/";

    public bool Changes => Action is ToolAction.Add or ToolAction.Update or ToolAction.Delete
        || (Action == ToolAction.Script && !ReadOnly);

    /// <summary>The file a script tool runs, in the package.</summary>
    public string ScriptPath => $"{ScriptFolder}{Name}.js";

    /// <summary>Takes its fields from JARVIS, with defaults and required ones.</summary>
    public bool Creates => Action is ToolAction.Add or ToolAction.Script;

    public bool NeedsKey => Action is ToolAction.Get or ToolAction.Update or ToolAction.Delete;
}
