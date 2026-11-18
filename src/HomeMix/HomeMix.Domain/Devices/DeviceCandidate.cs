namespace HomeMix.Domain.Devices;

/// <summary>What the matcher needs to know about a household device. Capabilities are as Devices lists them (<c>switch:lampka</c> for an instance).</summary>
public sealed record DeviceCandidate(
    Guid Id, string Name, bool Online, string? Model, IReadOnlyList<string> Roles, IReadOnlyList<string> Capabilities);
