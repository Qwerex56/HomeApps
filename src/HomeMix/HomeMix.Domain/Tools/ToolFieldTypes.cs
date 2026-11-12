using System.Text.Json;

namespace HomeMix.Domain.Tools;

/// <summary>The field types a declared tool takes, as in JSON Schema.</summary>
public static class ToolFieldTypes
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal) { "string", "integer", "number", "boolean" };

    /// <summary>Whether a JSON value is of the type (a default in the manifest, an argument in a call).</summary>
    public static bool Fits(string type, JsonElement value) => type switch
    {
        "string" => value.ValueKind == JsonValueKind.String,
        "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
        "number" => value.ValueKind == JsonValueKind.Number,
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        _ => false,
    };
}
