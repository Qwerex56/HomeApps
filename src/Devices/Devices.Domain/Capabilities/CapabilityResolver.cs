using Devices.Domain.Descriptors;

namespace Devices.Domain.Capabilities;

/// <summary>
/// Decides which of a descriptor's capabilities count (D13, D14). A built-in one counts when the device really has its
/// fields; a namespaced one counts when the descriptor carries its definition and the device has those fields. Anything
/// else (an unknown name, a namespaced one without a definition, missing fields) is ignored with a warning, and the
/// device still pairs. Unknown roles are ignored with a warning the same way.
/// </summary>
public static class CapabilityResolver
{
    public static CapabilityResolution Resolve(DeviceDescriptor descriptor, CapabilityCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(catalog);
        var accepted = new List<string>();
        var warnings = new List<string>();
        var register = new List<CapabilityDefinition>();

        foreach (var capability in descriptor.Capabilities)
        {
            var definition = capability.IsNamespaced ? capability.Definition : catalog.Find(capability.Type);
            if (definition is null)
            {
                warnings.Add(capability.IsNamespaced
                    ? $"Capability '{capability.Type}' ignored: a namespaced capability has to bring its definition."
                    : $"Capability '{capability.Type}' ignored: it is not in the catalog.");
                continue;
            }

            var missing = Missing(descriptor, definition, capability.Id);
            if (missing.Count > 0)
            {
                warnings.Add($"Capability '{capability.Key}' ignored: the device lacks {string.Join(", ", missing)}.");
                continue;
            }

            accepted.Add(capability.Key);
            if (capability.IsNamespaced && register.TrueForAll(d => d.Name != definition.Name))
            {
                register.Add(definition);
            }
        }

        var roles = descriptor.Roles.Where(catalog.HasRole).ToList();
        warnings.AddRange(descriptor.Roles.Except(roles).Select(r => $"Role '{r}' ignored: it is not in the catalog."));
        return new CapabilityResolution(accepted, roles, warnings, register);
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
