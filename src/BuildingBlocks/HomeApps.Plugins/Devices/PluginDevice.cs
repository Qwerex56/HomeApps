namespace HomeApps.Plugins.Devices;

/// <summary>A household device: what it is, what it can do and, when asked for, how it is now.</summary>
public sealed record PluginDevice(
    Guid Id,
    string Name,
    string Slug,
    string Purpose,
    bool Online,
    string? Model,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Capabilities,
    PluginDeviceState? State);
