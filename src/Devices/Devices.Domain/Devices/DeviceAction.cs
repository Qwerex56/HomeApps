using System.Text.Json;
using System.Text.RegularExpressions;
using FluentResults;

namespace Devices.Domain.Devices;

/// <summary>
/// Something a device can do for JARVIS, as the device announced it (jarvis-satellites.md 2): a name, what it does, the
/// JSON schema of its input, whether it only reads and whether it wants the person's "tak" first (S6). Whether the
/// person said "Nie pytaj" is the person's, kept across reconnects.
/// </summary>
public sealed partial class DeviceAction
{
    public const int MaxNameLength = 20;
    public const int MaxDescriptionLength = 300;
    public const int MaxSchemaLength = 4000;

    private DeviceAction() { }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>A JSON schema of type object, or <c>null</c> for an action without input.</summary>
    public string? InputSchema { get; private set; }

    public bool ReadOnly { get; private set; }

    /// <summary>The device wants a confirmation before the action runs.</summary>
    public bool Confirm { get; private set; }

    /// <summary>"Nie pytaj" (S6): the person allowed it in advance, so no confirmation is asked.</summary>
    public bool AutoApprove { get; private set; }

    /// <summary>JARVIS asks "tak/nie" first: a change the device marked, unless allowed in advance.</summary>
    public bool NeedsConfirmation => !ReadOnly && Confirm && !AutoApprove;

    public static Result<DeviceAction> Create(string name, string? description, string? inputSchema, bool readOnly, bool confirm)
    {
        name = name?.Trim() ?? string.Empty;
        description = description?.Trim() ?? string.Empty;
        inputSchema = string.IsNullOrWhiteSpace(inputSchema) ? null : inputSchema.Trim();
        if (!NamePattern().IsMatch(name) || description.Length > MaxDescriptionLength || !IsObjectSchema(inputSchema))
        {
            return Result.Fail(DeviceErrors.InvalidAction);
        }

        return new DeviceAction
        {
            Name = name,
            Description = description,
            InputSchema = inputSchema,
            ReadOnly = readOnly,
            Confirm = confirm,
        };
    }

    internal void AllowInAdvance(bool allowed) => AutoApprove = allowed;

    private static bool IsObjectSchema(string? schema)
    {
        if (schema is null)
        {
            return true;
        }

        if (schema.Length > MaxSchemaLength)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(schema);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{0,19}$")]
    private static partial Regex NamePattern();
}
