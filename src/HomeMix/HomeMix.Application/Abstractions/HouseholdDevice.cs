using System.Text.Json;
using HomeMix.Domain.Devices;

namespace HomeMix.Application.Abstractions;

/// <summary>A household device as an add-on sees it; <see cref="State"/> is Devices' state object when it was asked for.</summary>
public sealed record HouseholdDevice(
    Guid Id,
    string Name,
    string Slug,
    string Purpose,
    bool Online,
    string? Model,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Capabilities,
    JsonElement? State)
{
    public DeviceCandidate ToCandidate() => new(Id, Name, Online, Model, Roles, Capabilities);
}
