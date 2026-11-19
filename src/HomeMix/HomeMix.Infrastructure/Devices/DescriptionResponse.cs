using System.Text.Json;
using HomeMix.Application.Abstractions;

namespace HomeMix.Infrastructure.Devices;

/// <summary>One device of Devices' <c>GET /internal/households/{id}/devices</c>; <c>State</c> is the last values object.</summary>
internal sealed record DescriptionResponse(
    Guid Id, string Name, string Slug, string Purpose, bool Online, string[]? Capabilities, string? Model, string[]? Roles, JsonElement? State)
{
    public HouseholdDevice ToDevice() =>
        new(Id, Name, Slug, Purpose, Online, Model, Roles ?? [], Capabilities ?? [], State is { ValueKind: JsonValueKind.Object } ? State : null);
}
