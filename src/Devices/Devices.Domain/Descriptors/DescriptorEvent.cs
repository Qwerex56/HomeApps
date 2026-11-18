namespace Devices.Domain.Descriptors;

/// <summary>
/// Something that happens and is recorded (doorbell, motion); <see cref="Data"/> is the JSON Schema of its payload, if any.
/// <see cref="Location"/>: it says where something is (a tag seen at the door), so it is kept shorter (D9).
/// </summary>
public sealed record DescriptorEvent(string Name, string? Label, string? Description, string? Data, bool Notify, bool Location = false);
