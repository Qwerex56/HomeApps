namespace HomeMix.Domain.Manifests;

/// <summary>
/// Names an add-on id may never take: its id prefixes its JARVIS tools, and the gateway already uses these as source
/// names (jarvis-v1.md 10a). Plugin ids from <c>HOMEAPPS_PLUGINS</c> are added by the service at runtime.
/// </summary>
public static class ReservedAddonIds
{
    public static IReadOnlySet<string> Core { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "households", "accounts", "jarvis", "homemix", "notifications", "auth",
    };
}
