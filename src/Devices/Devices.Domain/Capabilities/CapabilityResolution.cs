namespace Devices.Domain.Capabilities;

/// <summary>
/// What a descriptor's capabilities came to: the <see cref="Accepted"/> ones (<c>weather</c>, <c>switch:lampka</c>), the known <see cref="Roles"/>, the
/// <see cref="Warnings"/> for those ignored (shown on the device later) and the <see cref="ToRegister"/> definitions of
/// namespaced capabilities that the instance should know.
/// </summary>
public sealed record CapabilityResolution(
    IReadOnlyList<string> Accepted, IReadOnlyList<string> Roles, IReadOnlyList<string> Warnings, IReadOnlyList<CapabilityDefinition> ToRegister);
