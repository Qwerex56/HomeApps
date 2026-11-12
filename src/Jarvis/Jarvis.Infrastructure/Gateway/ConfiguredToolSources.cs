using System.Net.Http.Json;
using System.Text.Json;
using Jarvis.Application.Gateway;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure.Gateway;

/// <summary>
/// The core sources from <see cref="GatewayOptions.Sources"/> and the plugins of <c>HOMEAPPS_PLUGINS</c> whose manifest
/// names an MCP server (<c>endpoints.mcp</c>). Manifests are public and the same for everyone, so they are kept for a
/// few minutes; a plugin that does not answer is left out until it does.
/// </summary>
internal sealed partial class ConfiguredToolSources(
    HttpClient http, IOptions<GatewayOptions> options, IMemoryCache cache, ILogger<ConfiguredToolSources> logger) : IToolSources
{
    private static readonly TimeSpan ManifestLifetime = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<ToolSource>> ListAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var sources = settings.Sources
            .Select(s => new ToolSource(s.Name, s.Prefix ?? s.Name, new Uri(s.Url)))
            .ToList();

        foreach (var id in settings.PluginIds)
        {
            if (await PluginSourceAsync(id, settings.PluginBaseUrl(id), cancellationToken) is { } plugin)
            {
                sources.Add(plugin);
            }
        }

        // A tool name without a prefix is HomeMix's only after every prefixed source said it is not theirs.
        return [.. sources.OrderBy(s => s.Prefix.Length == 0)];
    }

    private async Task<ToolSource?> PluginSourceAsync(string id, string baseUrl, CancellationToken cancellationToken)
    {
        var key = (nameof(ConfiguredToolSources), id);
        if (cache.TryGetValue(key, out ToolSource? known))
        {
            return known;
        }

        try
        {
            var manifest = await http.GetFromJsonAsync<JsonElement>($"{baseUrl}/plugins/{id}/manifest", cancellationToken);
            var path = manifest.TryGetProperty("endpoints", out var endpoints) && endpoints.TryGetProperty("mcp", out var mcp)
                && mcp.ValueKind == JsonValueKind.String ? mcp.GetString() : null;
            var source = path is null ? null : new ToolSource(id, id, new Uri($"{baseUrl}{path}"));
            cache.Set(key, source, ManifestLifetime);
            return source;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogNoManifest(id, exception);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The manifest of plugin {PluginId} did not load; its tools are left out for now.")]
    private partial void LogNoManifest(string pluginId, Exception exception);
}
