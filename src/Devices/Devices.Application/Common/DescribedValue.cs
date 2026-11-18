using System.Text.Json;

namespace Devices.Application.Common;

/// <summary>A control or a sensor of a device as it is described to JARVIS: its name, texts and the JSON Schema of its value.</summary>
public sealed record DescribedValue(string Name, string? Label, string? Description, JsonElement Schema, string? Unit);
