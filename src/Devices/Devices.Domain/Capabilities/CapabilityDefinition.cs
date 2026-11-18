using System.Text.Json;
using System.Text.RegularExpressions;
using Devices.Domain.Descriptors;

namespace Devices.Domain.Capabilities;

/// <summary>
/// A capability's contract (household-jarvis-devices.md 2.5): fixed field names with the JSON Schema of their values. The
/// built-in ones come from the embedded catalog, a namespaced one (<c>author/name</c>, D14) from the descriptor that
/// first brings it. <see cref="AnyOf"/> lists names of which at least one must be present (a weather station has some of
/// temperature, humidity, pressure, not necessarily all).
/// </summary>
public sealed partial record CapabilityDefinition(
    string Name, int Version, string? Description, IReadOnlyList<CapabilityField> Fields, IReadOnlyList<string> AnyOf)
{
    public const int MaxFields = 30;
    public const int MaxVersion = 10000;
    public const int MaxDescription = 300;

    /// <summary>Parses <c>{ version, description, anyOf, fields: [{ kind, name, value, required }] }</c>; <paramref name="problems"/> gets what is wrong.</summary>
    public static CapabilityDefinition? Parse(JsonElement json, string name, List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(problems);
        var start = problems.Count;
        if (json.ValueKind != JsonValueKind.Object)
        {
            problems.Add($"capability '{name}': definition must be an object");
            return null;
        }

        var version = json.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) && n >= 1 && n <= MaxVersion ? n : 0;
        if (version == 0)
        {
            problems.Add($"capability '{name}': definition.version must be an integer from 1 to {MaxVersion}");
        }

        var fields = new List<CapabilityField>();
        if (json.TryGetProperty("fields", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                var kind = item.ValueKind == JsonValueKind.Object ? Text(item, "kind") switch
                {
                    "control" => CapabilityFieldKind.Control,
                    "sensor" => CapabilityFieldKind.Sensor,
                    "event" => CapabilityFieldKind.Event,
                    _ => (CapabilityFieldKind?)null,
                } : null;
                var fieldName = item.ValueKind == JsonValueKind.Object ? Text(item, "name") : null;
                string? value = null;
                var valueOk = !item.TryGetProperty("value", out var schema) || (JsonSchemas.IsValueSchema(schema) && (value = schema.GetRawText()) is not null);
                if (kind is null || fieldName is null || !FieldName().IsMatch(fieldName) || !valueOk)
                {
                    problems.Add($"capability '{name}': every field needs kind (control, sensor, event), a lowercase name and a value schema");
                    continue;
                }

                var required = !item.TryGetProperty("required", out var r) || r.ValueKind != JsonValueKind.False;
                fields.Add(new CapabilityField(kind.Value, fieldName, value, required));
            }
        }

        if (fields.Count == 0 && name.Contains('/', StringComparison.Ordinal))
        {
            problems.Add($"capability '{name}': definition needs at least one field");
        }

        if (fields.Count > MaxFields || fields.Select(f => (f.Kind, f.Name)).Distinct().Count() != fields.Count)
        {
            problems.Add($"capability '{name}': definition has at most {MaxFields} fields, each once");
        }

        var anyOf = new List<string>();
        if (json.TryGetProperty("anyOf", out var any) && any.ValueKind == JsonValueKind.Array)
        {
            var items = any.EnumerateArray().ToList();
            if (items.Count > MaxFields || items.Exists(e => e.ValueKind != JsonValueKind.String))
            {
                problems.Add($"capability '{name}': anyOf is a list of field names (at most {MaxFields})");
            }
            else
            {
                anyOf.AddRange(items.Select(e => e.GetString() ?? string.Empty));
            }
        }

        if (anyOf.Exists(a => !fields.Exists(f => f.Name == a)))
        {
            problems.Add($"capability '{name}': anyOf names a field that is not defined");
        }

        var description = Text(json, "description");
        if (description is { Length: > MaxDescription })
        {
            problems.Add($"capability '{name}': description is at most {MaxDescription} characters");
        }

        return problems.Count > start ? null : new CapabilityDefinition(name, version, description, fields, anyOf);
    }

    /// <summary>
    /// A newer version only extends (D14): the fields it adds are kept, those already registered stay as they were,
    /// and the version moves up. An older or equal version changes nothing.
    /// </summary>
    public CapabilityDefinition ExtendedBy(CapabilityDefinition newer)
    {
        ArgumentNullException.ThrowIfNull(newer);
        if (newer.Version <= Version)
        {
            return this;
        }

        var addedList = newer.Fields.Where(f => !Fields.Any(o => o.Kind == f.Kind && o.Name == f.Name)).ToList();
        if (Fields.Count + addedList.Count > MaxFields)
        {
            return this;
        }

        return this with { Version = newer.Version, Fields = [.. Fields, .. addedList.Select(f => f with { Required = false })] };
    }

    /// <summary>The same shape <see cref="Parse"/> reads, as stored for a registered capability.</summary>
    public string ToJson()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", Version);
            if (Description is not null)
            {
                writer.WriteString("description", Description);
            }

            writer.WriteStartArray("anyOf");
            AnyOf.ToList().ForEach(writer.WriteStringValue);
            writer.WriteEndArray();
            writer.WriteStartArray("fields");
            foreach (var field in Fields)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", field.Kind.ToString().ToLowerInvariant());
                writer.WriteString("name", field.Name);
                if (field.Value is not null)
                {
                    writer.WritePropertyName("value");
                    writer.WriteRawValue(field.Value);
                }

                writer.WriteBoolean("required", field.Required);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Reads what <see cref="ToJson"/> wrote.</summary>
    public static CapabilityDefinition FromStored(string name, string json)
    {
        var problems = new List<string>();
        using var document = JsonDocument.Parse(json);
        return Parse(document.RootElement, name, problems) ?? throw new InvalidOperationException($"Stored capability '{name}' is invalid: {string.Join("; ", problems)}");
    }

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    [GeneratedRegex("^[a-z][a-z0-9_]{0,29}$")]
    private static partial Regex FieldName();
}
