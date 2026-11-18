namespace Devices.Domain.Descriptors;

/// <summary>A value that can be read and set; <see cref="Value"/> is its JSON Schema.</summary>
public sealed record DescriptorControl(string Name, string? Label, string? Description, string Value, string? Widget);
