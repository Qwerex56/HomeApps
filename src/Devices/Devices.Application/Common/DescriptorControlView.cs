using System.Text.Json;

namespace Devices.Application.Common;

/// <summary>A control of a device's descriptor as the household tab shows it; <paramref name="Value"/> is its JSON Schema.</summary>
public sealed record DescriptorControlView(string Name, string? Label, string? Description, JsonElement Value, string? Widget);
