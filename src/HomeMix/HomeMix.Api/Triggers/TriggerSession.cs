using System.Text.Json;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeMix.Api.Mcp;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Triggers;
using HomeMix.Domain.Devices;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Triggers;

namespace HomeMix.Api.Triggers;

/// <summary>
/// One trigger run's <c>ctx</c> calls: <c>devices.list</c>, <c>devices.state</c> and <c>devices.set</c> (only with <c>devices.control</c>),
/// asked of Devices as the installer within the add-on's consent. Jint calls it synchronously, so each call waits for Devices; the run's
/// thread is its own (<see cref="TriggerScriptRunner"/>). Answers are JSON: <c>{"value": …}</c> or <c>{"error": {"code", "message"}}</c>.
/// </summary>
internal sealed class TriggerSession(TriggerRun run, IAddonDevices devices, CancellationToken cancellationToken)
{
    private int _calls;

    public string Call(string method, string argumentsJson)
    {
        try
        {
            if (++_calls > ScriptLimits.HostCalls)
            {
                return Error("homemix.script_limit", $"At most {ScriptLimits.HostCalls} ctx calls in one run.");
            }

            using var parsed = JsonDocument.Parse(argumentsJson);
            var args = parsed.RootElement.EnumerateArray().Select(a => a.Clone()).ToList();
            return method switch
            {
                "devices.list" => List(Arg(args, 0)),
                "devices.state" => State(Id(args)),
                "devices.set" => Set(Id(args), Text(args, 1), Arg(args, 2)),
                _ => Error("homemix.unknown_method", $"ctx has no {method} in a trigger."),
            };
        }
        catch (JsonException e)
        {
            return Error("homemix.bad_request", e.Message);
        }
        catch (ScriptArgumentException e)
        {
            return Error("homemix.bad_request", e.Message);
        }
        catch (Exception e) when (e is HttpRequestException or InvalidOperationException && !cancellationToken.IsCancellationRequested)
        {
            return Error("homemix.devices_unavailable", e.Message);
        }
    }

    private Guid Household => run.Installation.TargetId;

    private Guid Installer => run.Installation.InstalledBy;

    private string List(JsonElement filter)
    {
        var rule = new DeviceMatch(Field(filter, "capability"), Field(filter, "role"), Field(filter, "model"), null);
        var withState = filter.ValueKind == JsonValueKind.Object && filter.TryGetProperty("state", out var s) && s.ValueKind == JsonValueKind.True;
        var found = devices.ListAsync(Household, Installer, cancellationToken).GetAwaiter().GetResult()
            .Where(d => DeviceMatcher.Fits(rule, d.ToCandidate()));
        return Value(found.Select(d => new
        {
            id = d.Id,
            name = d.Name,
            slug = d.Slug,
            model = d.Model,
            roles = d.Roles,
            capabilities = d.Capabilities,
            online = d.Online,
            state = withState ? d.State : null,
        }));
    }

    private string State(Guid id)
    {
        var device = devices.ListAsync(Household, Installer, cancellationToken).GetAwaiter().GetResult().FirstOrDefault(d => d.Id == id);
        return Value(device?.State);
    }

    private string Set(Guid id, string control, JsonElement value)
    {
        if (!run.Installation.Capabilities.Contains(AddonCapabilities.DevicesControl))
        {
            return Error(TriggerErrors.ControlMissing);
        }

        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new ScriptArgumentException("A value is required.");
        }

        var set = devices.SetAsync(run.Installation, run.Manifest.Name.Pl, id, control, value, cancellationToken).GetAwaiter().GetResult();
        return set.IsSuccess ? Value(null) : Error(set.Errors[0]);
    }

    private static string? Field(JsonElement filter, string name) =>
        filter.ValueKind == JsonValueKind.Object && filter.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static JsonElement Arg(List<JsonElement> args, int index) => index < args.Count ? args[index] : default;

    private static Guid Id(List<JsonElement> args) =>
        Text(args, 0) is var text && Guid.TryParse(text, out var id) ? id : throw new ScriptArgumentException("Argument 1 must be a device id.");

    private static string Text(List<JsonElement> args, int index) =>
        Arg(args, index) is { ValueKind: JsonValueKind.String } text
            ? text.GetString()!
            : throw new ScriptArgumentException($"Argument {index + 1} must be a string.");

    private static string Value(object? value) => JsonSerializer.Serialize(new { value }, ScriptBridge.Json);

    private static string Error(IError error) =>
        Error(error.Metadata.TryGetValue(AppError.CodeKey, out var code) && code is string text ? text : "homemix.error", error.Message);

    private static string Error(string code, string message) => JsonSerializer.Serialize(new { error = new { code, message } }, ScriptBridge.Json);
}
