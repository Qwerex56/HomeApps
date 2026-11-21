using System.Text.Json;

namespace Devices.Application.Common;

/// <summary>A sensor of a device's descriptor as the household tab shows it; <paramref name="Value"/> is its JSON Schema.</summary>
public sealed record DescriptorSensorView(string Name, string? Label, JsonElement Value, string? Unit);

