namespace Devices.Domain.Descriptors;

/// <summary>A read-only value; <see cref="Value"/> is its JSON Schema.</summary>
public sealed record DescriptorSensor(string Name, string? Label, string? Description, string Value, string? Unit);
