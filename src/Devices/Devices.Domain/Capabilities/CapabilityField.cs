namespace Devices.Domain.Capabilities;

/// <summary>A field a capability fixes: its kind, name, the JSON Schema of its value (or event data) and whether a device must have it.</summary>
public sealed record CapabilityField(CapabilityFieldKind Kind, string Name, string? Value, bool Required);
