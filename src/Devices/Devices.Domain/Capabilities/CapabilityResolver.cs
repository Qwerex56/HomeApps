using Devices.Domain.Descriptors;

namespace Devices.Domain.Capabilities;

/// <summary>
/// Decides which of a descriptor's capabilities count (D13, D14). A built-in one counts when the device really has its
/// fields; a namespaced one counts when it is registered (and not hidden) or its author's household device brings the definition, and the device has those fields. Anything
/// else (an unknown name, a namespaced one without a definition, missing fields) is ignored with a warning, and the
/// device still pairs. Unknown roles are ignored with a warning the same way.
/// </summary>
public static class CapabilityResolver
{
    public static CapabilityResolution Resolve(
        DeviceDescriptor descriptor, CapabilityCatalog catalog, IReadOnlyDictionary<string, RegisteredCapability> registered, bool isHouseholdDevice)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(registered);
        var modelAuthor = descriptor.Model.Split('/')[0];
        var accepted = new List<string>();
        var warnings = new List<string>();
        var register = new List<CapabilityDefinition>();

        foreach (var capability in descriptor.Capabilities)
        {
            CapabilityDefinition? brings = null;
            var definition = capability.IsNamespaced ? Namespaced(capability, modelAuthor, isHouseholdDevice, registered, warnings, out brings) : catalog.Find(capability.Type);
            if (!capability.IsNamespaced && definition is null)
            {
                warnings.Add($"Capability '{capability.Type}' ignored: it is not in the catalog.");
            }

            if (definition is null)
            {
                continue;
            }

            var missing = Missing(descriptor, definition, capability.Id);
            if (missing.Count > 0)
            {
                warnings.Add($"Capability '{capability.Key}' ignored: the device lacks {string.Join(", ", missing)}.");
                continue;
            }

            accepted.Add(capability.Key);
            if (brings is not null && register.TrueForAll(d => d.Name != brings.Name))
            {
                register.Add(brings);
            }
        }

        var roles = descriptor.Roles.Where(catalog.HasRole).ToList();
        warnings.AddRange(descriptor.Roles.Except(roles).Select(r => $"Role '{r}' ignored: it is not in the catalog."));
        return new CapabilityResolution(accepted, roles, warnings, register);
    }

    /// <summary>
    /// The definition a namespaced capability counts against: the registered one (D14: it wins; the device's own only extends
    /// it when the namespace author is the model author), or the device's own when nobody registered it yet and the author
    /// and a household device allow it. <paramref name="brings"/> is what the instance should register or extend with.
    /// </summary>
    private static CapabilityDefinition? Namespaced(
        DescriptorCapability capability, string modelAuthor, bool isHouseholdDevice,
        IReadOnlyDictionary<string, RegisteredCapability> registered, List<string> warnings, out CapabilityDefinition? brings)
    {
        brings = null;
        var ownsNamespace = capability.Type.Split('/')[0] == modelAuthor;
        if (registered.TryGetValue(capability.Type, out var known))
        {
            if (known.Hidden)
            {
                warnings.Add($"Capability '{capability.Type}' ignored: the owner hid it.");
                return null;
            }

            var current = known.Definition;
            if (!ownsNamespace || capability.Definition is null)
            {
                return current;
            }

            brings = capability.Definition;
            return current.ExtendedBy(capability.Definition);
        }

        if (capability.Definition is null)
        {
            warnings.Add($"Capability '{capability.Type}' ignored: a namespaced capability has to bring its definition.");
            return null;
        }

        if (!ownsNamespace || !isHouseholdDevice)
        {
            warnings.Add($"Capability '{capability.Type}' ignored: only a household device of the namespace's author can register it.");
            return null;
        }

        brings = capability.Definition;
        return capability.Definition;
    }

    /// <summary>What the capability promises and the device does not have (or has with another type).</summary>
    private static List<string> Missing(DeviceDescriptor descriptor, CapabilityDefinition definition, string? id)
    {
        var prefix = id is null ? string.Empty : id + ".";
        var missing = new List<string>();
        foreach (var field in definition.Fields)
        {
            var declared = Declared(descriptor, field.Kind, prefix + field.Name);
            if (declared is null)
            {
                if (field.Required)
                {
                    missing.Add($"{field.Kind.ToString().ToLowerInvariant()} '{prefix}{field.Name}'");
                }
            }
            else if (!SameType(field.Value, declared.Value.Schema))
            {
                missing.Add($"{field.Kind.ToString().ToLowerInvariant()} '{prefix}{field.Name}' of the right type");
            }
        }

        if (definition.AnyOf.Count > 0 && !definition.AnyOf.Any(name => Declared(descriptor, definition.Fields.First(f => f.Name == name).Kind, prefix + name) is not null))
        {
            missing.Add($"any of {string.Join("/", definition.AnyOf.Select(n => prefix + n))}");
        }

        return missing;
    }

    private static (string? Schema, bool Found)? Declared(DeviceDescriptor descriptor, CapabilityFieldKind kind, string name) => kind switch
    {
        CapabilityFieldKind.Control => descriptor.Controls.FirstOrDefault(c => c.Name == name) is { } c ? (c.Value, true) : null,
        CapabilityFieldKind.Sensor => descriptor.Sensors.FirstOrDefault(s => s.Name == name) is { } s ? (s.Value, true) : null,
        _ => descriptor.Events.FirstOrDefault(e => e.Name == name) is { } e ? (e.Data, true) : null,
    };

    /// <summary>The device's value type fits the catalog's: equal, or an integer where a number is asked. No type on either side fits.</summary>
    private static bool SameType(string? expected, string? actual)
    {
        var want = expected is null ? null : JsonSchemas.TypeOf(expected);
        var have = actual is null ? null : JsonSchemas.TypeOf(actual);
        return want is null || have is null || want == have || (want == "number" && have == "integer");
    }
}
