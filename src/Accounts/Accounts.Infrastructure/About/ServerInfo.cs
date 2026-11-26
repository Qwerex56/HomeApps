using System.Net.Http.Json;
using System.Text.Json;
using Accounts.Application.Abstractions;
using Accounts.Application.Admin.GetServerAbout;

namespace Accounts.Infrastructure.About;

/// <summary>
/// Reads <c>/health/version</c> of every service over the Docker network, the plugins' manifests and the newest GitHub
/// release. Every call has a 3-second limit: what does not answer is shown as such, never as an error.
/// </summary>
internal sealed class ServerInfo(HttpClient http, string[] pluginIds) : IServerInfo
{
    private static readonly string[] Services = ["auth", "accounts", "households", "notifications", "homemix", "devices", "jarvis"];
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan CacheFor = TimeSpan.FromHours(1);
    private const string LatestReleaseUrl = "https://api.github.com/repos/qwerex56/HomeApps/releases/latest";

    private readonly Lock _gate = new();
    private DateTimeOffset _cachedAt;
    private string? _cachedTag;

    public async Task<IReadOnlyList<ServiceAboutView>> ServicesAsync(CancellationToken cancellationToken) =>
        await Task.WhenAll(Services.Select(async name =>
        {
            var reply = await TryGetAsync<VersionReply>($"http://{name}:8080/health/version", cancellationToken);
            return new ServiceAboutView(
                char.ToUpperInvariant(name[0]) + name[1..],
                reply?.Version,
                reply?.Migrations?.Count ?? 0,
                reply?.Migrations?.LastOrDefault());
        }));

    public async Task<IReadOnlyList<PluginAboutView>> PluginsAsync(CancellationToken cancellationToken) =>
        await Task.WhenAll(pluginIds.Select(async id =>
        {
            var manifest = await TryGetAsync<ManifestReply>($"http://{id}:8080/manifest", cancellationToken);
            return new PluginAboutView(id, manifest?.Version, manifest is null ? "unavailable" : "healthy");
        }));

    public async Task<string?> LatestReleaseTagAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_cachedTag is not null && DateTimeOffset.UtcNow - _cachedAt < CacheFor)
            {
                return _cachedTag;
            }
        }

        var release = await TryGetAsync<ReleaseReply>(LatestReleaseUrl, cancellationToken);
        if (release?.TagName is null)
        {
            return null;
        }

        lock (_gate)
        {
            _cachedTag = release.TagName;
            _cachedAt = DateTimeOffset.UtcNow;
        }

        return release.TagName;
    }

    private async Task<T?> TryGetAsync<T>(string url, CancellationToken cancellationToken)
        where T : class
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(Limit);
        try
        {
            return await http.GetFromJsonAsync<T>(url, JsonSerializerOptions.Web, limit.Token);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or JsonException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private sealed record VersionReply(string? Version, List<string>? Migrations);

    private sealed record ManifestReply(string? Version);

    private sealed record ReleaseReply(string? TagName);
}
