using System.Text.Json;
using System.Text.RegularExpressions;
using HomeMix.Domain.Data;
using HomeMix.Domain.Tools;

namespace HomeMix.Domain.Manifests;

/// <summary>
/// <c>tools</c> and <c>summary</c> of homemix.json (A6), checked against the add-on's own capabilities and permissions;
/// problems go to the same list as the rest of the manifest's.
/// </summary>
internal sealed partial class ToolsReader(
    JsonElement root, string addonId, IReadOnlyCollection<string> capabilities, IReadOnlyCollection<AddonPermission> permissions,
    List<string> problems)
{
    public const int MaxTools = 20;
    public const int MaxFields = 20;

    /// <summary>The name JARVIS sees is <c>{addonId}_{name}</c>, at most 64 characters (MCP's limit).</summary>
    public const int MaxExposedName = 64;

    private static readonly Dictionary<string, ToolAction> Actions = new(StringComparer.Ordinal)
    {
        ["add"] = ToolAction.Add,
        ["list"] = ToolAction.List,
        ["get"] = ToolAction.Get,
        ["update"] = ToolAction.Update,
        ["delete"] = ToolAction.Delete,
    };

    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal) { "key", "household" };

    [GeneratedRegex("^[a-z][a-z0-9_]{0,39}$")]
    private static partial Regex ToolNamePattern();

    [GeneratedRegex("^[a-z][A-Za-z0-9_]{0,39}$")]
    private static partial Regex FieldNamePattern();

    public List<AddonTool> Tools()
    {
        var tools = new List<AddonTool>();
        if (!root.TryGetProperty("tools", out var element))
        {
            return tools;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            problems.Add("tools must be an array.");
            return tools;
        }

        if (element.GetArrayLength() > MaxTools)
        {
            problems.Add($"tools: at most {MaxTools}.");
        }

        foreach (var tool in element.EnumerateArray())
        {
            if (tool.ValueKind != JsonValueKind.Object)
            {
                problems.Add("tools may hold only objects.");
                continue;
            }

            if (Tool(tool) is { } read)
            {
                tools.Add(read);
            }
        }

        if (tools.Select(t => t.Name).Distinct(StringComparer.Ordinal).Count() != tools.Count)
        {
            problems.Add("tools: every name must be different.");
        }

        return tools;
    }

    public AddonSummary? Summary()
    {
        if (!root.TryGetProperty("summary", out var element))
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            problems.Add("summary must be an object with a collection.");
            return null;
        }

        var collection = Text(element, "collection") ?? string.Empty;
        if (!DataNames.IsValid(collection))
        {
            problems.Add("summary.collection must be a collection name.");
        }

        var scope = Scope(element, "summary");
        var description = Text(element, "description");
        if (description is { Length: > 500 })
        {
            problems.Add("summary.description: at most 500 characters.");
        }

        return new AddonSummary(collection, scope, description);
    }

    private AddonTool? Tool(JsonElement tool)
    {
        var name = Text(tool, "name") ?? string.Empty;
        var at = $"tools['{name}']";
        if (!ToolNamePattern().IsMatch(name) || name == "summary")
        {
            problems.Add($"{at}.name must be 1-40 characters: lowercase letters, digits and '_', starting with a letter, not 'summary'.");
        }
        else if (addonId.Length + 1 + name.Length > MaxExposedName)
        {
            problems.Add($"{at}.name: '{addonId}_{name}' is longer than {MaxExposedName} characters.");
        }

        var description = Text(tool, "description");
        if (string.IsNullOrWhiteSpace(description) || description.Length > 500)
        {
            problems.Add($"{at}.description is required, at most 500 characters: JARVIS picks tools by it.");
        }

        var collection = Text(tool, "collection") ?? string.Empty;
        if (!DataNames.IsValid(collection))
        {
            problems.Add($"{at}.collection must be a collection name.");
        }

        var actionText = Text(tool, "action") ?? string.Empty;
        if (!Actions.TryGetValue(actionText, out var action))
        {
            problems.Add($"{at}.action must be add, list, get, update or delete.");
        }

        var scope = Scope(tool, at);
        var fields = Fields(tool, at);
        if (fields.Count > 0 && action is not (ToolAction.Add or ToolAction.Update))
        {
            problems.Add($"{at}.fields: only add and update tools take fields.");
        }

        var permission = Text(tool, "permission");
        if (permission is not null && permissions.All(p => p.Id != permission))
        {
            problems.Add($"{at}.permission: '{permission}' is not in permissions.");
        }

        var answer = Text(tool, "answer");
        if (answer is { Length: > 300 })
        {
            problems.Add($"{at}.answer: at most 300 characters.");
        }

        return new AddonTool(name, description ?? string.Empty, collection, action, scope, fields, permission, answer);
    }

    private DataScope Scope(JsonElement owner, string at)
    {
        var scope = Text(owner, "scope") ?? "household";
        if (scope is not ("household" or "user"))
        {
            problems.Add($"{at}.scope must be household or user.");
            return DataScope.Household;
        }

        var kind = scope == "user" ? DataScope.User : DataScope.Household;
        var capability = kind == DataScope.User ? AddonCapabilities.StorageUser : AddonCapabilities.StorageHousehold;
        if (!capabilities.Contains(capability))
        {
            problems.Add($"{at}.scope {scope} needs the capability {capability}.");
        }

        return kind;
    }

    private List<AddonToolField> Fields(JsonElement tool, string at)
    {
        var fields = new List<AddonToolField>();
        if (!tool.TryGetProperty("fields", out var element))
        {
            return fields;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            problems.Add($"{at}.fields must be an object of field name to {{ type, description }}.");
            return fields;
        }

        foreach (var field in element.EnumerateObject())
        {
            var where = $"{at}.fields.{field.Name}";
            if (!FieldNamePattern().IsMatch(field.Name) || Reserved.Contains(field.Name))
            {
                problems.Add($"{where}: a field name is 1-40 letters, digits and '_', starting with a lowercase letter, not key or household.");
            }

            if (field.Value.ValueKind != JsonValueKind.Object)
            {
                problems.Add($"{where} must be an object with a type.");
                continue;
            }

            var type = Text(field.Value, "type") ?? string.Empty;
            if (!ToolFieldTypes.All.Contains(type))
            {
                problems.Add($"{where}.type must be string, integer, number or boolean.");
            }

            var description = Text(field.Value, "description");
            if (description is { Length: > 300 })
            {
                problems.Add($"{where}.description: at most 300 characters.");
            }

            var required = field.Value.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.True;
            JsonElement? fallback = field.Value.TryGetProperty("default", out var d) ? d.Clone() : null;
            if (fallback is { } value && !ToolFieldTypes.Fits(type, value))
            {
                problems.Add($"{where}.default must be a {type}.");
            }

            fields.Add(new AddonToolField(field.Name, type, description, required, fallback));
        }

        if (fields.Count > MaxFields)
        {
            problems.Add($"{at}.fields: at most {MaxFields}.");
        }

        return fields;
    }

    private static string? Text(JsonElement owner, string property) =>
        owner.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
