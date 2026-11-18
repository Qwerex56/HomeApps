using System.Text.Json;
using System.Text.RegularExpressions;
using Devices.Domain.Capabilities;
using Devices.Domain.Devices;
using FluentResults;

namespace Devices.Domain.Descriptors;

/// <summary>
/// Reads and checks a <c>homeapps.device/1</c> document: format, model, version, purpose, roles, capabilities, the
/// <c>jarvis</c> text, controls, sensors, events and actions, with values described by JSON Schema. Every problem found
/// is reported at once, so a device author fixes them in one go. Whether a capability exists is not decided here
/// (<see cref="CapabilityResolver"/>), only that it is well formed.
/// </summary>
public static partial class DescriptorParser
{
    public const int MaxItems = 50;
    private const int MaxLabel = 60;
    private const int MaxDescription = 300;
    private static readonly HashSet<string> Widgets = ["switch", "slider", "select", "button"];

    public static Result<DeviceDescriptor> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > DeviceDescriptor.MaxLength)
        {
            return Fail($"the descriptor must be a JSON document of at most {DeviceDescriptor.MaxLength} characters");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return Fail("the descriptor is not valid JSON");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Fail("the descriptor must be a JSON object");
            }

            var problems = new List<string>();
            var descriptor = Read(root, problems);
            return problems.Count > 0 || descriptor is null ? Fail([.. problems]) : descriptor;
        }
    }

    private static DeviceDescriptor? Read(JsonElement root, List<string> problems)
    {
        if (Text(root, "format") != DeviceDescriptor.Format)
        {
            problems.Add($"format must be \"{DeviceDescriptor.Format}\"");
        }

        var model = Text(root, "model");
        if (model is null || model.Length > 100 || !ModelPattern().IsMatch(model))
        {
            problems.Add("model must look like \"author/name\" (lowercase letters, digits, '.', '_', '-'), at most 100 characters");
        }

        var version = Text(root, "version");
        if (version is null || !VersionPattern().IsMatch(version))
        {
            problems.Add("version must be a semantic version such as \"1.2.0\"");
        }

        if (!DevicePurposes.TryParse(Text(root, "purpose"), out var purpose))
        {
            problems.Add("purpose must be assistant, smart_home or computer");
        }

        var roles = Strings(root, "roles", problems, RolePattern(), "a role is lowercase letters, digits and '_'");
        var jarvis = Text(root, "jarvis");
        if (jarvis is { Length: > DeviceDescriptor.MaxJarvisLength })
        {
            problems.Add($"jarvis must be at most {DeviceDescriptor.MaxJarvisLength} characters");
        }

        int? heartbeat = null;
        if (root.TryGetProperty("heartbeat", out var beat))
        {
            if (beat.ValueKind == JsonValueKind.Number && beat.TryGetInt32(out var seconds) && seconds is >= 5 and <= 86_400)
            {
                heartbeat = seconds;
            }
            else
            {
                problems.Add("heartbeat must be a number of seconds from 5 to 86400");
            }
        }

        var sleepy = root.TryGetProperty("sleepy", out var s) && s.ValueKind == JsonValueKind.True;
        var capabilities = Items(root, "capabilities", problems, Capability);
        var controls = Items(root, "controls", problems, Control);
        var sensors = Items(root, "sensors", problems, Sensor);
        var events = Items(root, "events", problems, Event);
        var actions = Items(root, "actions", problems, Action);

        Unique(capabilities.Select(c => c.Key), "capability", problems);
        Unique(controls.Select(c => c.Name).Concat(sensors.Select(x => x.Name)), "control or sensor", problems);
        Unique(events.Select(e => e.Name), "event", problems);
        Unique(actions.Select(a => a.Name), "action", problems);

        return problems.Count > 0
            ? null
            : new DeviceDescriptor(root.GetRawText(), model!, version!, purpose, roles, capabilities, jarvis, heartbeat, sleepy, controls, sensors, events, actions);
    }

    private static DescriptorCapability? Capability(JsonElement item, List<string> problems)
    {
        var type = Text(item, "type");
        if (type is null || !(TypePattern().IsMatch(type) || NamespacedPattern().IsMatch(type)))
        {
            problems.Add("a capability needs a type: a catalog name (weather) or author/name");
            return null;
        }

        var id = Text(item, "id");
        if (id is not null && !IdPattern().IsMatch(id))
        {
            problems.Add($"capability '{type}': id is lowercase letters, digits and '_'");
            return null;
        }

        CapabilityDefinition? definition = null;
        if (type.Contains('/', StringComparison.Ordinal) && item.TryGetProperty("definition", out var raw))
        {
            definition = CapabilityDefinition.Parse(raw, type, problems);
        }

        return new DescriptorCapability(type, id, definition);
    }

    private static DescriptorControl? Control(JsonElement item, List<string> problems)
    {
        var name = FieldName(item, "control", problems);
        var value = Schema(item, "value", name, problems, required: true);
        var widget = Text(item, "widget");
        if (widget is not null && !Widgets.Contains(widget))
        {
            problems.Add($"control '{name}': widget must be switch, slider, select or button");
        }

        return name is null || value is null ? null : new DescriptorControl(name, Label(item, name, problems), Description(item, name, problems), value, widget);
    }

    private static DescriptorSensor? Sensor(JsonElement item, List<string> problems)
    {
        var name = FieldName(item, "sensor", problems);
        var value = Schema(item, "value", name, problems, required: true);
        var unit = Text(item, "unit");
        if (unit is { Length: > 20 })
        {
            problems.Add($"sensor '{name}': unit is at most 20 characters");
        }

        return name is null || value is null ? null : new DescriptorSensor(name, Label(item, name, problems), Description(item, name, problems), value, unit);
    }

    private static DescriptorEvent? Event(JsonElement item, List<string> problems)
    {
        var name = FieldName(item, "event", problems);
        var data = Schema(item, "data", name, problems, required: false);
        var notify = item.TryGetProperty("notify", out var n) && n.ValueKind == JsonValueKind.True;
        var location = item.TryGetProperty("location", out var l) && l.ValueKind == JsonValueKind.True;
        return name is null ? null : new DescriptorEvent(name, Label(item, name, problems), Description(item, name, problems), data, notify, location);
    }

    private static DescriptorAction? Action(JsonElement item, List<string> problems)
    {
        var name = Text(item, "name");
        if (name is null || !ActionNamePattern().IsMatch(name))
        {
            problems.Add("an action needs a name: lowercase letters, digits and '_', starting with a letter, at most 20 characters");
            return null;
        }

        string? input = null;
        if (item.TryGetProperty("input", out var schema))
        {
            if (JsonSchemas.IsObjectSchema(schema))
            {
                input = schema.GetRawText();
            }
            else
            {
                problems.Add($"action '{name}': input must be a JSON Schema of type object");
            }
        }

        var readOnly = item.TryGetProperty("readOnly", out var r) && r.ValueKind == JsonValueKind.True;
        var confirm = item.TryGetProperty("confirm", out var c) && c.ValueKind == JsonValueKind.True;
        return new DescriptorAction(name, Label(item, name, problems), Description(item, name, problems), input, readOnly, confirm);
    }

    private static string? FieldName(JsonElement item, string what, List<string> problems)
    {
        var name = Text(item, "name");
        if (name is not null && FieldNamePattern().IsMatch(name))
        {
            return name;
        }

        problems.Add($"a {what} needs a name: lowercase letters, digits and '_', starting with a letter (\"id.name\" for a capability instance)");
        return null;
    }

    private static string? Schema(JsonElement item, string property, string? name, List<string> problems, bool required)
    {
        if (!item.TryGetProperty(property, out var schema))
        {
            if (required && name is not null)
            {
                problems.Add($"'{name}': {property} (a JSON Schema) is required");
            }

            return null;
        }

        if (JsonSchemas.IsValueSchema(schema))
        {
            return schema.GetRawText();
        }

        problems.Add($"'{name}': {property} must be a JSON Schema object with a type, at most {DeviceDescriptor.MaxSchemaLength} characters");
        return null;
    }

    private static string? Label(JsonElement item, string name, List<string> problems)
    {
        var label = Text(item, "label");
        if (label is { Length: > MaxLabel })
        {
            problems.Add($"'{name}': label is at most {MaxLabel} characters");
        }

        return label;
    }

    private static string? Description(JsonElement item, string name, List<string> problems)
    {
        var description = Text(item, "description");
        if (description is { Length: > MaxDescription })
        {
            problems.Add($"'{name}': description is at most {MaxDescription} characters");
        }

        return description;
    }

    private static List<T> Items<T>(JsonElement root, string property, List<string> problems, Func<JsonElement, List<string>, T?> read)
        where T : class
    {
        var result = new List<T>();
        if (!root.TryGetProperty(property, out var array))
        {
            return result;
        }

        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > MaxItems)
        {
            problems.Add($"{property} must be an array of at most {MaxItems} items");
            return result;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                problems.Add($"every item of {property} must be an object");
            }
            else if (read(item, problems) is { } parsed)
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    private static List<string> Strings(JsonElement root, string property, List<string> problems, Regex pattern, string rule)
    {
        var result = new List<string>();
        if (!root.TryGetProperty(property, out var array))
        {
            return result;
        }

        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > 10)
        {
            problems.Add($"{property} must be an array of at most 10 strings");
            return result;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && pattern.IsMatch(item.GetString()!))
            {
                result.Add(item.GetString()!);
            }
            else
            {
                problems.Add($"{property}: {rule}");
            }
        }

        return [.. result.Distinct(StringComparer.Ordinal)];
    }

    private static void Unique(IEnumerable<string> names, string what, List<string> problems)
    {
        foreach (var duplicate in names.GroupBy(n => n, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            problems.Add($"{what} '{duplicate.Key}' is listed twice");
        }
    }

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static Result<DeviceDescriptor> Fail(params string[] problems) =>
        Result.Fail(DeviceErrors.InvalidDescriptor(problems));

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._-]*(/[a-z0-9][a-z0-9._-]*)?$")]
    private static partial Regex ModelPattern();

    [GeneratedRegex(@"^\d{1,4}(\.\d{1,4}){0,2}([-+][0-9A-Za-z.-]{1,30})?$")]
    private static partial Regex VersionPattern();

    [GeneratedRegex("^[a-z][a-z0-9_]{0,39}$")]
    private static partial Regex RolePattern();

    [GeneratedRegex("^[a-z][a-z0-9_]{0,29}$")]
    private static partial Regex TypePattern();

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,29}/[a-z][a-z0-9_-]{0,29}$")]
    private static partial Regex NamespacedPattern();

    [GeneratedRegex("^[a-z][a-z0-9_]{0,29}$")]
    private static partial Regex IdPattern();

    [GeneratedRegex(@"^[a-z][a-z0-9_]{0,29}(\.[a-z][a-z0-9_]{0,29})?$")]
    private static partial Regex FieldNamePattern();

    [GeneratedRegex("^[a-z][a-z0-9_]{0,19}$")]
    private static partial Regex ActionNamePattern();
}
