namespace Devices.Application.Common;

public sealed record DeviceView(
    Guid Id,
    string Name,
    string Slug,
    string Kind,
    string Purpose,
    Guid? HouseholdId,
    bool Paired,
    bool Online,
    bool Removed,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    IReadOnlyList<DeviceActionView> Actions,
    DeviceDescriptorView Descriptor,
    string? AdminNotes,
    bool JarvisMayControl = true,
    bool JarvisAsks = true,
    DeviceStateView? State = null);
