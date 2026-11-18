namespace Devices.Domain.Descriptors;

/// <summary>Something that happens and is recorded (doorbell, motion); <see cref="Data"/> is the JSON Schema of its payload, if any.</summary>
public sealed record DescriptorEvent(string Name, string? Label, string? Description, string? Data, bool Notify);
