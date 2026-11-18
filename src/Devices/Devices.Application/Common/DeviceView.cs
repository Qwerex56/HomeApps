namespace Devices.Application.Common;

public sealed record DeviceView(
    Guid Id,
    string Name,
    string Kind,
    bool Online,
    bool Removed,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    IReadOnlyList<DeviceActionView> Actions);
