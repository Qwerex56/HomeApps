using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using HomeMix.Application;
using HomeMix.Application.Abstractions;
using HomeMix.Infrastructure.GitHub;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMix.Infrastructure.Store;

/// <summary>
/// Reads <c>addons.json</c> and, for each repository in it, its description, latest release (the SHA-256 GitHub gives
/// for <c>addon.zip</c>) and README, three calls per repository, all kept for <c>HomeMix:StoreCacheMinutes</c>. A
/// repository GitHub cannot describe stays listed with its problem; an index that cannot be read gives an empty store.
/// </summary>
internal sealed partial class GitHubStoreIndex(
    HttpClient http, IMemoryCache cache, IOptions<HomeMixOptions> options, ILogger<GitHubStoreIndex> logger) : IStoreIndex
{
    private const string CacheKey = "homemix.store";

    /// <summary>The store lists at most this many repositories, to stay under GitHub's anonymous limit.</summary>
    public const int MaxRepositories = 15;

    public async Task<IReadOnlyList<StoreListing>> ListAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.StoreIndexUrl))
        {
            return [];
        }

        if (cache.TryGetValue(CacheKey, out IReadOnlyList<StoreListing>? cached) && cached is not null)
        {
            return cached;
        }

        var listings = await ReadAsync(settings.StoreIndexUrl, cancellationToken);
        cache.Set(CacheKey, listings, TimeSpan.FromMinutes(listings.Count == 0 ? 5 : settings.StoreCacheMinutes));
        return listings;
    }

    private async Task<IReadOnlyList<StoreListing>> ReadAsync(string indexUrl, CancellationToken cancellationToken)
    {
        StoreIndexFile? index;
        try
        {
            index = await http.GetFromJsonAsync<StoreIndexFile>(new Uri(indexUrl), cancellationToken);
        }
        catch (Exception e) when (e is HttpRequestException or System.Text.Json.JsonException or TaskCanceledException)
        {
            LogIndexFailed(logger, indexUrl, e.Message);
            return [];
        }

        var repositories = (index?.Addons ?? [])
            .Select(a => RepositoryPattern().Match(a.Repository?.Trim() ?? string.Empty))
            .Where(m => m.Success)
            .Select(m => (Owner: m.Groups["owner"].Value, Name: m.Groups["repo"].Value))
            .DistinctBy(r => $"{r.Owner}/{r.Name}".ToLowerInvariant())
            .Take(MaxRepositories);

        var listings = new List<StoreListing>();
        foreach (var (owner, name) in repositories)
        {
            listings.Add(await DescribeAsync(owner, name, cancellationToken));
        }

        return listings;
    }

    private async Task<StoreListing> DescribeAsync(string owner, string name, CancellationToken cancellationToken)
    {
        var link = $"https://github.com/{owner}/{name}";
        try
        {
            var repository = await http.GetFromJsonAsync<GitHubRepositoryResponse>($"repos/{owner}/{name}", cancellationToken);
            using var latest = await http.GetAsync($"repos/{owner}/{name}/releases/latest", cancellationToken);
            if (latest.StatusCode == HttpStatusCode.NotFound)
            {
                return new StoreListing(link, repository?.Name ?? name, repository?.Description, null, null, null, null, "no_release");
            }

            latest.EnsureSuccessStatusCode();
            var release = await latest.Content.ReadFromJsonAsync<GitHubReleaseResponse>(cancellationToken);
            var asset = release?.Assets.FirstOrDefault(a => a.Name == GitHubReleasesClient.AssetName);
            var sha256 = asset?.Digest is { } digest && digest.StartsWith("sha256:", StringComparison.Ordinal) ? digest["sha256:".Length..] : null;

            using var readmeResponse = await http.GetAsync($"repos/{owner}/{name}/readme", cancellationToken);
            var readme = readmeResponse.IsSuccessStatusCode
                ? (await readmeResponse.Content.ReadFromJsonAsync<GitHubReadmeResponse>(cancellationToken))?.Text
                : null;

            return new StoreListing(
                link, repository?.Name ?? name, repository?.Description, release?.TagName, sha256, Shorten(readme), release?.PublishedAt,
                asset is null ? "no_addon_zip" : sha256 is null ? "no_digest" : null);
        }
        catch (Exception e) when (e is HttpRequestException or System.Text.Json.JsonException or TaskCanceledException)
        {
            LogRepositoryFailed(logger, link, e.Message);
            return new StoreListing(link, name, null, null, null, null, null, "unavailable");
        }
    }

    /// <summary>A README is shown in the store as it is; a huge one is cut.</summary>
    private static string? Shorten(string? readme) => readme is { Length: > 20_000 } ? readme[..20_000] + "\n\n…" : readme;

    [GeneratedRegex(@"^https://github\.com/(?<owner>[A-Za-z0-9-]{1,39})/(?<repo>[A-Za-z0-9._-]{1,100}?)(\.git)?/?$", RegexOptions.IgnoreCase)]
    private static partial Regex RepositoryPattern();

    [LoggerMessage(Level = LogLevel.Warning, Message = "The HomeMix store index {Url} could not be read: {Problem}")]
    private static partial void LogIndexFailed(ILogger logger, string url, string problem);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The store entry {Repository} could not be read from GitHub: {Problem}")]
    private static partial void LogRepositoryFailed(ILogger logger, string repository, string problem);
}
