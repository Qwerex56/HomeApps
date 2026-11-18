namespace Devices.Domain.Capabilities;

/// <summary>
/// A namespaced capability (<c>qwerex56/akwarium</c>) that a device brought with its definition and that is now known
/// to the whole instance (D14: not per household, a contract for store add-ons). Hiding is the owner's; promotion to a
/// built-in is a release step, not done here.
/// </summary>
public sealed class RegisteredCapability
{
    private RegisteredCapability() { }

    public string Name { get; private set; } = string.Empty;

    public int Version { get; private set; }

    /// <summary>The definition as <see cref="CapabilityDefinition.ToJson"/> wrote it.</summary>
    public string DefinitionJson { get; private set; } = string.Empty;

    public bool Hidden { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public CapabilityDefinition Definition => CapabilityDefinition.FromStored(Name, DefinitionJson);

    public static RegisteredCapability Register(CapabilityDefinition definition, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new RegisteredCapability
        {
            Name = definition.Name,
            Version = definition.Version,
            DefinitionJson = definition.ToJson(),
            RegisteredAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>A device brought the definition again: only a newer version, and only adding fields, changes it.</summary>
    public void Extend(CapabilityDefinition seen, DateTimeOffset now)
    {
        var extended = Definition.ExtendedBy(seen);
        if (extended.Version != Version)
        {
            Version = extended.Version;
            DefinitionJson = extended.ToJson();
            UpdatedAt = now;
        }
    }

    public void Hide() => Hidden = true;

    public void Unhide() => Hidden = false;
}
