namespace Jarvis.Application.Gateway;

/// <summary>
/// What the household's JARVIS allows in that household (H-2, D1), as a ceiling for the satellite and for any person's
/// JARVIS acting there: a disabled assistant leaves only the core sources, <see cref="ReadOnly"/> allows no change.
/// </summary>
public sealed record HouseholdCeiling(bool Enabled, bool ReadOnly, bool Plugins, bool HomeMix, bool Devices)
{
    /// <summary>When Households cannot say: nothing but the core sources, nothing changed.</summary>
    public static HouseholdCeiling Closed { get; } = new(false, true, false, false, false);

    /// <summary>From Households' <c>assistant</c> view: <c>accessLevel</c> and the flags <c>toolSources</c> as names ("Plugins, HomeMix").</summary>
    public static HouseholdCeiling From(bool enabled, string? accessLevel, string? toolSources) => new(
        enabled,
        !string.Equals(accessLevel, "Member", StringComparison.OrdinalIgnoreCase),
        Has(toolSources, "Plugins"),
        Has(toolSources, "HomeMix"),
        Has(toolSources, "Devices"));

    public bool Allows(ToolSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Kind switch
        {
            ToolSourceKind.Plugins => Enabled && Plugins,
            ToolSourceKind.HomeMix => Enabled && HomeMix,
            ToolSourceKind.Devices => Enabled && Devices,
            _ => true,
        };
    }

    private static bool Has(string? flags, string name) =>
        flags is not null && flags.Split(',', StringSplitOptions.TrimEntries).Contains(name, StringComparer.OrdinalIgnoreCase);
}
