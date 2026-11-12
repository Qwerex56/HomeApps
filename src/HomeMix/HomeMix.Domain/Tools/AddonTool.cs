using HomeMix.Domain.Data;

namespace HomeMix.Domain.Tools;

/// <summary>
/// A tool declared in <c>homemix.json</c>: an operation on one collection of the add-on's data, which HomeMix serves
/// over MCP as <c>{addonId}_{Name}</c>. No add-on code runs on the server.
/// </summary>
/// <param name="Permission">An add-on permission the person's household role must have to see and call it.</param>
/// <param name="Answer">The text it answers, with <c>{field}</c> and <c>{key}</c> filled in.</param>
public sealed record AddonTool(
    string Name,
    string Description,
    string Collection,
    ToolAction Action,
    DataScope Scope,
    IReadOnlyList<AddonToolField> Fields,
    string? Permission,
    string? Answer)
{
    public bool Changes => Action is ToolAction.Add or ToolAction.Update or ToolAction.Delete;

    public bool NeedsKey => Action is ToolAction.Get or ToolAction.Update or ToolAction.Delete;
}
