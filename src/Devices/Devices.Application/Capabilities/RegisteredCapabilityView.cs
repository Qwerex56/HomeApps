namespace Devices.Application.Capabilities;

public sealed record RegisteredCapabilityView(
    string Name,
    int Version,
    string? Description,
    bool Hidden,
    DateTimeOffset RegisteredAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<RegisteredCapabilityFieldView> Fields);
