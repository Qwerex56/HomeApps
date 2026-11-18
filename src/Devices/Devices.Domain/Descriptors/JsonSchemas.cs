using System.Text.Json;

namespace Devices.Domain.Descriptors;

/// <summary>
/// The light check of a value schema in a descriptor: a JSON object of reasonable size that says what it is
/// (<c>type</c> one of the JSON types, or <c>enum</c>/<c>const</c>). Devices passes it on as is; it does not run a full validator.
/// </summary>
internal static class JsonSchemas
{
    private static readonly HashSet<string> Types = ["boolean", "integer", "number", "string", "object", "array", "null"];

    public static bool IsValueSchema(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object
        && schema.GetRawText().Length <= DeviceDescriptor.MaxSchemaLength
        && (schema.TryGetProperty("type", out var type)
            ? type.ValueKind == JsonValueKind.String && Types.Contains(type.GetString()!)
            : schema.TryGetProperty("enum", out _) || schema.TryGetProperty("const", out _));

    public static bool IsObjectSchema(JsonElement schema) =>
        IsValueSchema(schema) && schema.TryGetProperty("type", out var type) && type.GetString() == "object";

    /// <summary>The top-level <c>type</c>, or <c>null</c> when the schema has none.</summary>
    public static string? TypeOf(string schema)
    {
        using var document = JsonDocument.Parse(schema);
        return document.RootElement.TryGetProperty("type", out var type) ? type.GetString() : null;
    }
}
