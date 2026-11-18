using Devices.Domain.Capabilities;

namespace Devices.Domain.Descriptors;

/// <summary>
/// A capability the device claims: a built-in <c>type</c> (<c>weather</c>) or a namespaced one (<c>qwerex56/akwarium</c>)
/// that brings its <see cref="Definition"/>. <see cref="Id"/> tells apart several of one type; its fields are then <c>id.field</c>.
/// </summary>
public sealed record DescriptorCapability(string Type, string? Id, CapabilityDefinition? Definition)
{
    public bool IsNamespaced => Type.Contains('/', StringComparison.Ordinal);

    /// <summary>How the capability is listed on the device: <c>weather</c> or <c>switch:lampka</c>.</summary>
    public string Key => Id is null ? Type : $"{Type}:{Id}";
}
