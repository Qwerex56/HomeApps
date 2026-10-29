using HomeApps.Contracts.Households;

namespace HomeApps.Plugins.Manifest;

/// <summary>
/// The manifest as clients read it at <c>GET /plugins/{id}/manifest</c>: the web app, the hub and JARVIS. Every path is
/// absolute on the HomeApps host. Change it only by adding optional fields; anything else needs a new
/// <see cref="ManifestVersion"/>, which older web apps skip.
/// </summary>
public sealed record PluginManifestDocument(
    int ManifestVersion,
    string Id,
    string Version,
    PluginRequirementsDocument Requires,
    LocalizedText Name,
    LocalizedText? Description,
    string Icon,
    IReadOnlyList<DataScope> Scopes,
    PluginEnablementDocument? Enablement,
    IReadOnlyList<PluginPermission> Permissions,
    PluginEndpointsDocument Endpoints,
    PluginUiDocument? Ui,
    IReadOnlyList<DisplayScreen> Display,
    PluginMcp? Mcp,
    PluginEventsDocument Events)
{
    public const int CurrentVersion = 1;

    public static PluginManifestDocument From(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var basePath = manifest.BasePath;
        var api = $"{basePath}/api";

        return new PluginManifestDocument(
            CurrentVersion,
            manifest.Id,
            manifest.Version,
            new PluginRequirementsDocument(manifest.MinHomeAppsVersion),
            manifest.Name,
            manifest.Description,
            manifest.Icon,
            manifest.Scopes,
            manifest.Enablement is null ? null : new PluginEnablementDocument(DataScope.Household, manifest.Enablement.MinRole, $"{api}/households/{{householdId}}/enablement"),
            manifest.Permissions,
            new PluginEndpointsDocument(
                api,
                $"{basePath}/openapi/v1.json",
                manifest.Mcp is null ? null : $"{basePath}/mcp",
                $"{basePath}/health/ready"),
            manifest.Ui is null
                ? null
                : new PluginUiDocument($"{basePath}/ui/entry.js", $"{basePath}/ui/entry.css", manifest.Ui.Routes, manifest.Ui.Widgets, manifest.Ui.Settings),
            manifest.Display.Select(d => d with { Path = $"{api}/{d.Path}" }).ToList(),
            manifest.Mcp,
            new PluginEventsDocument(
                manifest.Events.Publishes.Select(t => t.FullName!).ToList(),
                manifest.Events.Consumes.Select(t => t.FullName!).ToList()));
    }
}

/// <param name="HomeApps">The oldest HomeApps version the plugin works with.</param>
public sealed record PluginRequirementsDocument(string HomeApps);

/// <param name="Path">GET for any member: is it on here; PUT <c>{ "enabled": true }</c> for <see cref="MinRole"/> and up.</param>
public sealed record PluginEnablementDocument(DataScope Scope, HouseholdRole MinRole, string Path);

/// <param name="Mcp"><c>null</c> when the plugin has no MCP server.</param>
public sealed record PluginEndpointsDocument(string Api, string OpenApi, string? Mcp, string Health);

/// <param name="Entry">The ES module the web app imports (docs/plugins.md, "UI").</param>
/// <param name="Style">Its stylesheet, when the build produced one.</param>
public sealed record PluginUiDocument(
    string Entry,
    string Style,
    IReadOnlyList<PluginRoute> Routes,
    IReadOnlyList<PluginWidget> Widgets,
    bool Settings);

/// <summary>Full .NET type names of the messages (e.g. <c>HomeApps.Contracts.Households.MemberLeft</c>).</summary>
public sealed record PluginEventsDocument(IReadOnlyList<string> Publishes, IReadOnlyList<string> Consumes);
