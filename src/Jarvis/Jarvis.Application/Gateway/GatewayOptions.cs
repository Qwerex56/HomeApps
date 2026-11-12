using System.ComponentModel.DataAnnotations;

namespace Jarvis.Application.Gateway;

/// <summary>The gateway's settings (section <c>Jarvis</c>, jarvis-v1.md 2.3 and 10a).</summary>
public sealed class GatewayOptions
{
    public const string SectionName = "Jarvis";

    /// <summary>
    /// The gateway's public address, e.g. <c>https://home-apps.dev/jarvis/mcp</c>: the <c>resource</c> MCP clients ask
    /// Auth for, so the <c>aud</c> of their tokens. Must be listed in Auth's <c>Auth:ConnectedApps:Resources</c>.
    /// </summary>
    [Required]
    public string Resource { get; set; } = string.Empty;

    /// <summary>Core sources: Households, Accounts, HomeMix (empty prefix: its tools already carry the add-on's id).</summary>
    public List<SourceOptions> Sources { get; set; } = [];

    /// <summary>The instance's plugins (<c>HOMEAPPS_PLUGINS</c>, comma separated); each one's manifest names its MCP server.</summary>
    public string Plugins { get; set; } = string.Empty;

    /// <summary>Where a plugin answers inside the Docker network; <c>{id}</c> is replaced. Overridden per plugin by <see cref="PluginUrls"/>.</summary>
    public string PluginUrl { get; set; } = "http://{id}:8080";

    /// <summary>Per plugin, e.g. <c>todo = http://localhost:5400</c> when it runs from the IDE.</summary>
    public Dictionary<string, string> PluginUrls { get; set; } = [];

    /// <summary>Auth inside the Docker network, for <c>POST /internal/delegation</c>.</summary>
    [Required]
    public string AuthUrl { get; set; } = "http://auth:8080";

    /// <summary>The <c>Service</c> client in Auth's <c>Auth:Clients</c>.</summary>
    [Required]
    public string ClientId { get; set; } = "jarvis-service";

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    public IEnumerable<string> PluginIds =>
        Plugins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public string PluginBaseUrl(string id) =>
        (PluginUrls.TryGetValue(id, out var url) ? url : PluginUrl.Replace("{id}", id, StringComparison.Ordinal)).TrimEnd('/');
}
