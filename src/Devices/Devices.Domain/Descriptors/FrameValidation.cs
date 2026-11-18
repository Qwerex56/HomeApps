using System.Text.Json;
using Devices.Domain.Devices;
using FluentResults;

namespace Devices.Domain.Descriptors;

/// <summary>
/// Checks what a device sends (<c>state</c>, <c>event</c>) and what the server sends it (<c>set</c>) against the device's
/// own descriptor, lightly (see <see cref="ValueSchema"/>). Every problem is reported at once.
/// </summary>
public static class FrameValidation
{
    public const int MaxStateLength = 16_000;
    public const int MaxEventDataLength = 4_000;

    /// <summary>A state frame: every name is a control or a sensor of the descriptor and its value fits that schema.</summary>
    public static Result CheckState(DeviceDescriptor descriptor, JsonElement? values)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (values is not { ValueKind: JsonValueKind.Object } state || !state.EnumerateObject().Any() || state.GetRawText().Length > MaxStateLength)
        {
            return Fail($"values must be a JSON object with at least one value, at most {MaxStateLength} characters");
        }

        var problems = new List<string>();
        foreach (var value in state.EnumerateObject())
        {
            var schema = descriptor.Controls.FirstOrDefault(c => c.Name == value.Name)?.Value
                ?? descriptor.Sensors.FirstOrDefault(s => s.Name == value.Name)?.Value;
            if (schema is null)
            {
                problems.Add($"'{value.Name}' is not a control or sensor of this device");
            }
            else if (ValueSchema.Check(schema, value.Value) is { } problem)
            {
                problems.Add($"'{value.Name}': {problem}");
            }
        }

        return problems.Count == 0 ? Result.Ok() : Fail([.. problems]);
    }

    /// <summary>A <c>set</c> (REST to device): the control exists and the value fits its schema.</summary>
    public static Result CheckSet(DeviceDescriptor descriptor, string? control, JsonElement? value)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var found = descriptor.Controls.FirstOrDefault(c => c.Name == control);
        if (found is null)
        {
            return Result.Fail(DeviceErrors.ControlNotFound);
        }

        if (value is null)
        {
            return Fail("value is required");
        }

        return ValueSchema.Check(found.Value, value.Value) is { } problem ? Fail($"'{control}': {problem}") : Result.Ok();
    }

    /// <summary>An event frame: the name is declared in <c>events</c> and its data, when sent, fits the declared schema.</summary>
    public static Result<DescriptorEvent> CheckEvent(DeviceDescriptor descriptor, string? name, JsonElement? data)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var declared = descriptor.Events.FirstOrDefault(e => e.Name == name);
        if (declared is null)
        {
            return Result.Fail(DeviceErrors.InvalidFrame([$"'{name}' is not an event of this device"]));
        }

        if (data is { ValueKind: not JsonValueKind.Null } payload)
        {
            if (payload.GetRawText().Length > MaxEventDataLength)
            {
                return Fail($"data is at most {MaxEventDataLength} characters");
            }

            if (declared.Data is { } schema && ValueSchema.Check(schema, payload) is { } problem)
            {
                return Fail($"'{name}': {problem}");
            }
        }

        return declared;
    }

    private static Result Fail(params string[] problems) => Result.Fail(DeviceErrors.InvalidFrame(problems));
}
