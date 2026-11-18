using Devices.Domain.Devices;

namespace Devices.Domain.Descriptors;

/// <summary>
/// What a device says about itself in <c>homeapps.device/1</c> (household-jarvis-devices.md 2.3). <see cref="Json"/> is
/// the document as received (compact), the source of truth that Devices stores; the rest is what it was parsed into.
/// </summary>
public sealed record DeviceDescriptor(
    string Json,
    string Model,
    string Version,
    DevicePurpose Purpose,
    IReadOnlyList<string> Roles,
    IReadOnlyList<DescriptorCapability> Capabilities,
    string? Jarvis,
    int? Heartbeat,
    bool Sleepy,
    IReadOnlyList<DescriptorControl> Controls,
    IReadOnlyList<DescriptorSensor> Sensors,
    IReadOnlyList<DescriptorEvent> Events,
    IReadOnlyList<DescriptorAction> Actions)
{
    public const string Format = "homeapps.device/1";
    public const int MaxLength = 64_000;
    public const int MaxJarvisLength = 500;
    public const int MaxSchemaLength = 4000;
}
