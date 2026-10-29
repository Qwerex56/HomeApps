using HomeApps.Contracts.Households;

namespace HomeApps.Plugins.Manifest;

/// <summary>
/// What a plugin declares about itself, in code, once (docs/plugins.md). Clients never see this type: the plugin serves
/// <see cref="PluginManifestDocument"/>, which adds the paths of the API, UI, MCP and screens under
/// <c>/plugins/{Id}</c>. The manifest describes the plugin and points at its API; it is not the API.
/// </summary>
public sealed record PluginManifest
{
    /// <summary>Lower-case id, also the path prefix <c>/plugins/{Id}</c> and the queue name prefix, e.g. <c>todo</c>.</summary>
    public required string Id { get; init; }

    /// <summary>The plugin's own version (semantic version).</summary>
    public required string Version { get; init; }

    /// <summary>Oldest HomeApps version the plugin works with; the web app skips the plugin on an older instance.</summary>
    public required string MinHomeAppsVersion { get; init; }

    public required LocalizedText Name { get; init; }

    public LocalizedText? Description { get; init; }

    /// <summary>A PrimeIcons class, e.g. <c>pi-check-square</c>.</summary>
    public required string Icon { get; init; }

    /// <summary>Whose data the plugin keeps (jarvis-hub.md 9b.6). One plugin may have several.</summary>
    public required IReadOnlyList<DataScope> Scopes { get; init; }

    /// <summary>Who switches the plugin on in a household. Required for <see cref="DataScope.Household"/> and
    /// <see cref="DataScope.HouseholdUser"/> data; <c>null</c> means always on.</summary>
    public PluginEnablementRule? Enablement { get; init; }

    /// <summary>Named permissions, each tied to the lowest household role that has it.</summary>
    public IReadOnlyList<PluginPermission> Permissions { get; init; } = [];

    /// <summary>Screens the plugin adds to the web app; <c>null</c> for a plugin without UI.</summary>
    public PluginUi? Ui { get; init; }

    /// <summary>Compact JSON for e-paper and small screens (jarvis-hub.md 9b.2).</summary>
    public IReadOnlyList<DisplayScreen> Display { get; init; } = [];

    /// <summary>The plugin's MCP server for JARVIS; <c>null</c> when it has none.</summary>
    public PluginMcp? Mcp { get; init; }

    /// <summary>Integration messages the plugin publishes and consumes. Documentation only, not enforced.</summary>
    public PluginEvents Events { get; init; } = PluginEvents.None;

    /// <summary>Where everything of this plugin lives behind Traefik.</summary>
    public string BasePath => $"/plugins/{Id}";

    public PluginPermission? FindPermission(string id) =>
        Permissions.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));
}

/// <summary>A text in every language of the web app (frontend.md: Polish and English).</summary>
public sealed record LocalizedText(string Pl, string En);

/// <summary>Whose data it is and who decides about access (jarvis-hub.md 9b.6).</summary>
public enum DataScope
{
    /// <summary>One person's, whatever household they are in.</summary>
    User = 0,

    /// <summary>One household's, shared by its members according to their household roles.</summary>
    Household = 1,

    /// <summary>The whole instance's, read by every active account, changed by server admins.</summary>
    Application = 2,

    /// <summary>In a household, but an item may belong to one person (shared items for members, private ones for the author).</summary>
    HouseholdUser = 3,
}

/// <summary>Switched on and off per household by a member with at least <see cref="MinRole"/>.</summary>
public sealed record PluginEnablementRule(HouseholdRole MinRole);

public sealed record PluginPermission(string Id, HouseholdRole MinRole, LocalizedText? Description = null);

public sealed record PluginUi
{
    /// <summary>Pages in a household, under <c>/homes/{householdId}/apps/{pluginId}/{Path}</c> in the web app.</summary>
    public IReadOnlyList<PluginRoute> Routes { get; init; } = [];

    public IReadOnlyList<PluginWidget> Widgets { get; init; } = [];

    /// <summary>Whether the UI module brings its own settings page for the household's settings.</summary>
    public bool Settings { get; init; }
}

/// <param name="Path">Relative, without a leading slash; empty for the plugin's main page.</param>
/// <param name="Menu">Whether the page gets a tab in the household's menu.</param>
public sealed record PluginRoute(string Path, LocalizedText Title, bool Menu = true);

public sealed record PluginWidget(string Id, LocalizedText Title, WidgetSize Size, IReadOnlyList<WidgetPlace> Places);

public enum WidgetSize
{
    Small = 0,
    Medium = 1,
    Large = 2,
}

/// <summary>Where a widget may stand.</summary>
public enum WidgetPlace
{
    /// <summary>The household's page.</summary>
    Household = 0,

    /// <summary>The wall tablet (<c>/wall</c>).</summary>
    Wall = 1,

    /// <summary>The signed-in person's start page.</summary>
    Dashboard = 2,
}

/// <param name="Path">Relative to the plugin's API, without a leading slash; may hold <c>{householdId}</c>.</param>
/// <param name="RefreshSeconds">How often a screen should fetch it.</param>
public sealed record DisplayScreen(string Id, LocalizedText Title, string Path, int RefreshSeconds);

/// <param name="Tools">Names of the MCP tools, so JARVIS knows what to expect before it connects.</param>
public sealed record PluginMcp(IReadOnlyList<string> Tools);

public sealed record PluginEvents(IReadOnlyList<Type> Publishes, IReadOnlyList<Type> Consumes)
{
    public static PluginEvents None { get; } = new([], []);
}
