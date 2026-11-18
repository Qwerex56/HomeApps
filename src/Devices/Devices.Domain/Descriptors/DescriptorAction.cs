namespace Devices.Domain.Descriptors;

/// <summary>A one-off command; <see cref="Input"/> is a JSON Schema of type object, or <c>null</c>.</summary>
public sealed record DescriptorAction(string Name, string? Label, string? Description, string? Input, bool ReadOnly, bool Confirm);
