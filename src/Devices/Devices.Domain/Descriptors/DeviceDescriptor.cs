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

    /// <summary>Seconds between status reports when the descriptor does not say: a device on a permanent link.</summary>
    public const int DefaultHeartbeat = 60;

    /// <summary>The same for a sleepy (battery) device, which should declare its wake interval; 15 minutes when it does not.</summary>
    public const int DefaultSleepyHeartbeat = 900;

    /// <summary>The seconds between the device's status reports: declared, otherwise by whether it sleeps.</summary>
    public int HeartbeatSeconds => Heartbeat ?? (Sleepy ? DefaultSleepyHeartbeat : DefaultHeartbeat);

    /// <summary>An event is a location event when it is declared so, or when the device has the <c>geo</c> capability (D9).</summary>
    public bool IsLocation(DescriptorEvent descriptorEvent)
    {
        ArgumentNullException.ThrowIfNull(descriptorEvent);
        return descriptorEvent.Location || Capabilities.Any(c => c.Type == "geo");
    }
}
