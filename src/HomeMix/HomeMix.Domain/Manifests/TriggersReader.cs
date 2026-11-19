using System.Text.Json;
using System.Text.RegularExpressions;

namespace HomeMix.Domain.Manifests;

/// <summary>Reads <c>homemix.json</c> <c>triggers</c> for <see cref="ManifestReader"/>, adding every problem it finds to the shared list.</summary>
internal sealed partial class TriggersReader(JsonElement root, List<string> problems)
{
    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_.]{0,60}$")]
    private static partial Regex FieldPattern();

    public List<AddonTrigger> Triggers()
    {
        var triggers = new List<AddonTrigger>();
        if (!root.TryGetProperty("triggers", out var element))
        {
            return triggers;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            problems.Add("triggers must be an array.");
            return triggers;
        }

        if (element.GetArrayLength() > AddonTrigger.MaxPerAddon)
        {
            problems.Add($"triggers: at most {AddonTrigger.MaxPerAddon}.");
        }

        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            var at = $"triggers[{index++}]";
            if (item.ValueKind != JsonValueKind.Object)
            {
                problems.Add($"{at} must be an object.");
            }
            else if (Trigger(item, at) is { } trigger)
            {
                if (triggers.Exists(t => t.Id == trigger.Id))
                {
                    problems.Add($"{at}: the id '{trigger.Id}' is used twice; give each trigger its own id or script.");
                }

                triggers.Add(trigger);
            }
        }

        return triggers;
    }

    private AddonTrigger? Trigger(JsonElement item, string at)
    {
        var count = problems.Count;
        var on = Text(item, "on", at) switch
        {
            "state" => (TriggerKind?)TriggerKind.State,
            "event" => TriggerKind.Event,
            _ => null,
        };
        if (on is null)
        {
            problems.Add($"{at}.on must be \"state\" or \"event\".");
        }

        var capability = Pattern(item, "capability", ManifestReader.CapabilityPattern(), "a capability name such as weather or qwerex56/akwarium", at);
        var role = Pattern(item, "role", ManifestReader.RolePattern(), "a role such as weather_station", at);
        var model = Pattern(item, "model", ManifestReader.ModelPattern(), "a model such as qwerex56/fotel-v1", at);
        if (capability is null && role is null && model is null && problems.Count == count)
        {
            problems.Add($"{at}: needs a capability, a role or a model, like a widget's devices.");
        }

        var field = Pattern(item, "field", FieldPattern(), "a state value's name such as temperature", at);
        var name = Pattern(item, "name", FieldPattern(), "an event's name such as pressed", at);
        if (on == TriggerKind.Event && field is not null)
        {
            problems.Add($"{at}.field is for \"state\" triggers; an event trigger takes name.");
        }

        if (on == TriggerKind.State && name is not null)
        {
            problems.Add($"{at}.name is for \"event\" triggers; a state trigger takes field.");
        }

        var script = Text(item, "script", at);
        if (script is null || !ManifestReader.PathPattern().IsMatch(script) || script.Contains("..", StringComparison.Ordinal)
            || !script.StartsWith(AddonTrigger.ScriptFolder, StringComparison.Ordinal) || !script.EndsWith(".js", StringComparison.Ordinal)
            || script.Length <= AddonTrigger.ScriptFolder.Length + 3)
        {
            problems.Add($"{at}.script must be a path to a .js file under {AddonTrigger.ScriptFolder} in the package, e.g. {AddonTrigger.ScriptFolder}cold.js.");
            script = null;
        }

        var id = Text(item, "id", at, required: false) ?? (script is null ? null : Path.GetFileNameWithoutExtension(script));
        if (id is null || !ManifestReader.NamePattern().IsMatch(id))
        {
            problems.Add($"{at}.id must be a name of lowercase letters, digits, '.', '_' and '-' (it defaults to the script's file name).");
        }

        return problems.Count != count || on is null || script is null || id is null
            ? null
            : new AddonTrigger(id, on.Value, new DeviceMatch(capability, role, model, null), field, name, script);
    }

    private string? Text(JsonElement item, string property, string at, bool required = true)
    {
        if (!item.TryGetProperty(property, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        if (required || value.ValueKind != JsonValueKind.Null)
        {
            problems.Add($"{at}.{property} must be a string.");
        }

        return null;
    }

    private string? Pattern(JsonElement item, string property, Regex pattern, string what, string at)
    {
        var text = Text(item, property, at, required: false);
        if (text is null)
        {
            return null;
        }

        if (pattern.IsMatch(text))
        {
            return text;
        }

        problems.Add($"{at}.{property} must be {what}.");
        return null;
    }
}
