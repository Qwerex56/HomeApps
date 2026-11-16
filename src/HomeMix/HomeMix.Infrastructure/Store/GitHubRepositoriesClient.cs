using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using HomeMix.Application.Abstractions;
using HomeMix.Infrastructure.GitHub;
using Microsoft.Extensions.Logging;

namespace HomeMix.Infrastructure.Store;

/// <summary>
/// A repository's latest release and README from GitHub's REST API, without downloading <c>addon.zip</c>: the store
/// compares the SHA-256 GitHub gives with the package it has. Any failure is a problem code, never an exception.
/// </summary>
internal sealed partial class GitHubRepositoriesClient(HttpClient http, ILogger<GitHubRepositoriesClient> logger) : IGitHubRepositories
{
    public async Task<GitHubLatest> LatestAsync(string repository, CancellationToken cancellationToken)
    {
        var link = Normalize(repository);
        if (link is null)
        {
            return new GitHubLatest(null, null, null, null, null, "invalid_link");
        }

        var path = link["https://github.com/".Length..];
        try
        {
            using var latest = await http.GetAsync($"repos/{path}/releases/latest", cancellationToken);
            if (latest.StatusCode == HttpStatusCode.NotFound)
            {
                return new GitHubLatest(link, null, null, null, null, "no_release");
            }

            latest.EnsureSuccessStatusCode();
            var release = await latest.Content.ReadFromJsonAsync<GitHubReleaseResponse>(cancellationToken);
            var asset = release?.Assets.FirstOrDefault(a => a.Name == GitHubReleasesClient.AssetName);
            var sha256 = asset?.Digest is { } digest && digest.StartsWith("sha256:", StringComparison.Ordinal) ? digest["sha256:".Length..] : null;

            using var readmeResponse = await http.GetAsync($"repos/{path}/readme", cancellationToken);
            var readme = readmeResponse.IsSuccessStatusCode
                ? (await readmeResponse.Content.ReadFromJsonAsync<GitHubReadmeResponse>(cancellationToken))?.Text
                : null;

            var problem = asset is null ? "no_addon_zip" : sha256 is null ? "no_digest" : null;
            return new GitHubLatest(link, release?.TagName, sha256, release?.PublishedAt, readme, problem);
        }
        catch (Exception e) when (e is HttpRequestException or System.Text.Json.JsonException or TaskCanceledException or FormatException)
        {
            LogRepositoryFailed(logger, link, e.Message);
            return new GitHubLatest(link, null, null, null, null, "unavailable");
        }
    }

    /// <summary>https://github.com/owner/repo from a pasted link (a trailing slash or .git dropped), or null.</summary>
    public static string? Normalize(string? repository)
    {
        var match = RepositoryPattern().Match(repository?.Trim() ?? string.Empty);
        return match.Success ? $"https://github.com/{match.Groups["owner"].Value}/{match.Groups["repo"].Value}" : null;
    }

    [GeneratedRegex(@"^https://github\.com/(?<owner>[A-Za-z0-9-]{1,39})/(?<repo>[A-Za-z0-9._-]{1,100}?)(\.git)?/?$", RegexOptions.IgnoreCase)]
    private static partial Regex RepositoryPattern();

    [LoggerMessage(Level = LogLevel.Warning, Message = "The store entry {Repository} could not be read from GitHub: {Problem}")]
    private static partial void LogRepositoryFailed(ILogger logger, string repository, string problem);
}
