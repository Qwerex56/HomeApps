using System.Text.Json;
using HomeMix.Application.Abstractions;

namespace HomeMix.Infrastructure.Devices;

/// <summary>One device of <c>GET /devices/households/{id}</c>; the state is kept as Devices wrote it.</summary>
internal sealed record DeviceResponse(
    Guid Id, string Name, string Slug, string Purpose, bool Online, bool Removed, DescriptorResponse? Descriptor, JsonElement? State)
{
    public HouseholdDevice ToDevice() =>
        new(Id, Name, Slug, Purpose, Online, Descriptor?.Model, Descriptor?.Roles ?? [], Descriptor?.Capabilities ?? [], State is { ValueKind: JsonValueKind.Object } ? State : null);
}
