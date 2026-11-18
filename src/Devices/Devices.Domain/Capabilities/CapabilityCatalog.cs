using System.Text.Json;

namespace Devices.Domain.Capabilities;

/// <summary>
/// The built-in capabilities and roles (D13), loaded from the JSON embedded in this assembly: data, not classes. A new
/// one is a change to <c>builtin-capabilities.json</c> (and <c>docs/device-capabilities.md</c>), shipped in a release.
/// </summary>
public sealed class CapabilityCatalog
{
    public static readonly CapabilityCatalog BuiltIn = Load();

    private readonly Dictionary<string, CapabilityDefinition> _capabilities;
    private readonly HashSet<string> _roles;

    private CapabilityCatalog(IEnumerable<CapabilityDefinition> capabilities, IEnumerable<string> roles)
    {
        _capabilities = capabilities.ToDictionary(c => c.Name, StringComparer.Ordinal);
        _roles = [.. roles];
    }

    public IReadOnlyCollection<CapabilityDefinition> Capabilities => _capabilities.Values;

    public IReadOnlyCollection<string> Roles => _roles;

    public CapabilityDefinition? Find(string name) => _capabilities.GetValueOrDefault(name);

    public bool HasRole(string role) => _roles.Contains(role);

    /// <summary>Builds a catalog from JSON of the embedded shape; used for the embedded resource and by tests.</summary>
    public static CapabilityCatalog Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var problems = new List<string>();
        var capabilities = document.RootElement.GetProperty("capabilities").EnumerateArray()
            .Select(c => CapabilityDefinition.Parse(c, c.GetProperty("name").GetString()!, problems))
            .ToList();
        if (problems.Count > 0)
        {
            throw new InvalidOperationException("The built-in capability catalog is invalid: " + string.Join("; ", problems));
        }

        var roles = document.RootElement.GetProperty("roles").EnumerateArray().Select(r => r.GetProperty("name").GetString()!);
        return new CapabilityCatalog(capabilities.Select(c => c!), roles);
    }

    private static CapabilityCatalog Load()
    {
        using var stream = typeof(CapabilityCatalog).Assembly.GetManifestResourceStream("builtin-capabilities.json")
            ?? throw new InvalidOperationException("builtin-capabilities.json is not embedded.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }
}
