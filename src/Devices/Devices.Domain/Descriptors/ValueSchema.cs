using System.Text.Json;

namespace Devices.Domain.Descriptors;

/// <summary>
/// The light check of a value against a descriptor's JSON Schema, in the same spirit as <see cref="JsonSchemas"/>: <c>type</c>,
/// <c>enum</c>, <c>const</c>, <c>minimum</c>/<c>maximum</c>, <c>minLength</c>/<c>maxLength</c>, an object's <c>required</c> and
/// <c>properties</c> (followed recursively). Everything else in the schema is passed on, not enforced.
/// </summary>
public static class ValueSchema
{
    /// <summary>The problem with <paramref name="value"/>, or <c>null</c> when it fits the schema.</summary>
    public static string? Check(string schema, JsonElement value)
    {
        using var document = JsonDocument.Parse(schema);
        return Check(document.RootElement, value, "value");
    }

    private static string? Check(JsonElement schema, JsonElement value, string path)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (schema.TryGetProperty("const", out var constant) && !Same(constant, value))
        {
            return $"{path} must be {constant.GetRawText()}";
        }

        if (schema.TryGetProperty("enum", out var options) && options.ValueKind == JsonValueKind.Array && !options.EnumerateArray().Any(o => Same(o, value)))
        {
            return $"{path} must be one of {options.GetRawText()}";
        }

        if (schema.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && !HasType(type.GetString()!, value))
        {
            return $"{path} must be of type {type.GetString()}";
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => CheckNumber(schema, value, path),
            JsonValueKind.String => CheckString(schema, value, path),
            JsonValueKind.Object => CheckObject(schema, value, path),
            _ => null,
        };
    }

    private static string? CheckNumber(JsonElement schema, JsonElement value, string path)
    {
        var number = value.GetDouble();
        if (schema.TryGetProperty("minimum", out var min) && min.ValueKind == JsonValueKind.Number && min.TryGetDouble(out var low) && number < low)
        {
            return $"{path} must be at least {low}";
        }

        return schema.TryGetProperty("maximum", out var max) && max.ValueKind == JsonValueKind.Number && max.TryGetDouble(out var high) && number > high
            ? $"{path} must be at most {high}"
            : null;
    }

    private static string? CheckString(JsonElement schema, JsonElement value, string path)
    {
        var length = value.GetString()!.Length;
        if (schema.TryGetProperty("minLength", out var min) && min.ValueKind == JsonValueKind.Number && min.TryGetInt32(out var low) && length < low)
        {
            return $"{path} must be at least {low} characters";
        }

        return schema.TryGetProperty("maxLength", out var max) && max.ValueKind == JsonValueKind.Number && max.TryGetInt32(out var high) && length > high
            ? $"{path} must be at most {high} characters"
            : null;
    }

    private static string? CheckObject(JsonElement schema, JsonElement value, string path)
    {
        if (schema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var name in required.EnumerateArray().Where(n => n.ValueKind == JsonValueKind.String))
            {
                if (!value.TryGetProperty(name.GetString()!, out _))
                {
                    return $"{path} needs '{name.GetString()}'";
                }
            }
        }

        if (schema.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in properties.EnumerateObject())
            {
                if (value.TryGetProperty(property.Name, out var inner) && Check(property.Value, inner, $"{path}.{property.Name}") is { } problem)
                {
                    return problem;
                }
            }
        }

        return null;
    }

    private static bool HasType(string type, JsonElement value) => type switch
    {
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "integer" => value.ValueKind == JsonValueKind.Number && (value.TryGetInt64(out _) || IsWhole(value.GetDouble())),
        "number" => value.ValueKind == JsonValueKind.Number,
        "string" => value.ValueKind == JsonValueKind.String,
        "object" => value.ValueKind == JsonValueKind.Object,
        "array" => value.ValueKind == JsonValueKind.Array,
        "null" => value.ValueKind == JsonValueKind.Null,
        _ => true,
    };

    private static bool IsWhole(double number) => Math.Abs(number % 1) < double.Epsilon;

    private static bool Same(JsonElement left, JsonElement right) => left.GetRawText() == right.GetRawText()
        || (left.ValueKind == JsonValueKind.Number && right.ValueKind == JsonValueKind.Number && left.GetDouble() == right.GetDouble());
}
