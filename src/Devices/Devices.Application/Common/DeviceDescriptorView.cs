namespace Devices.Application.Common;

/// <summary>What a device's descriptor says, in short: who made it, what it can do and what was ignored. <see cref="Described"/> is false for a legacy device without one.</summary>
public sealed record DeviceDescriptorView(
    bool Described,
    string? Model,
    string? Version,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Warnings);
