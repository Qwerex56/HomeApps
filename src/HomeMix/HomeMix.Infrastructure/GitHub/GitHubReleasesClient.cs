using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FluentResults;
using HomeMix.Application;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Packages;
using Microsoft.Extensions.Options;

namespace HomeMix.Infrastructure.GitHub;

/// <summary>
/// Reads a repository's latest (or named) release through GitHub's REST API, downloads its <c>addon.zip</c> and
/// checks it against the SHA-256 digest GitHub publishes for the asset (W9). No digest, no install.
/// </summary>
internal sealed partial class GitHubReleasesClient(HttpClient http, IOptions<HomeMixOptions> options) : IGitHubReleases
{
    public const string AssetName = "addon.zip";

    public async Task<Result<GitHubRelease>> DownloadAsync(string repository, string? tag, CancellationToken cancellationToken)
    {
        var match = RepositoryPattern().Match(repository.Trim());
        if (!match.Success)
        {
            return Result.Fail(PackageErrors.Download("Paste a GitHub repository link such as https://github.com/owner/repo."));
        }

        var (owner, name) = (match.Groups["owner"].Value, match.Groups["repo"].Value);
        var path = string.IsNullOrWhiteSpace(tag)
            ? $"repos/{owner}/{name}/releases/latest"
            : $"repos/{owner}/{name}/releases/tags/{Uri.EscapeDataString(tag.Trim())}";

        using var response = await http.GetAsync(path, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Result.Fail(PackageErrors.Download($"{owner}/{name} has no such public release on GitHub."));
        }

        if (!response.IsSuccessStatusCode)
        {
            return Result.Fail(PackageErrors.Download($"GitHub answered {(int)response.StatusCode}; try again later."));
        }

        var release = await response.Content.ReadFromJsonAsync<GitHubReleaseResponse>(cancellationToken);
        var asset = release?.Assets.FirstOrDefault(a => string.Equals(a.Name, AssetName, StringComparison.Ordinal));
        if (release is null || asset is null)
        {
            return Result.Fail(PackageErrors.Download($"The release has no {AssetName}; build it with the HomeMix template's workflow."));
        }

        if (asset.Digest is null || !asset.Digest.StartsWith("sha256:", StringComparison.Ordinal))
        {
            return Result.Fail(PackageErrors.Download($"GitHub gives no SHA-256 for {AssetName} of this release."));
        }

        var max = options.Value.MaxPackageBytes;
        if (asset.Size > max)
        {
            return Result.Fail(PackageErrors.TooLarge(max));
        }

        using var download = await http.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!download.IsSuccessStatusCode)
        {
            return Result.Fail(PackageErrors.Download($"Downloading {AssetName} failed ({(int)download.StatusCode})."));
        }

        var zip = await ReadLimitedAsync(download.Content, max, cancellationToken);
        if (zip is null)
        {
            return Result.Fail(PackageErrors.TooLarge(max));
        }

        var sha256 = Convert.ToHexStringLower(SHA256.HashData(zip));
        if (!string.Equals(sha256, asset.Digest["sha256:".Length..], StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail(PackageErrors.Download($"{AssetName} does not match the SHA-256 GitHub gives for it."));
        }

        return new GitHubRelease($"https://github.com/{owner}/{name}", release.TagName, zip, sha256);
    }

    private static async Task<byte[]?> ReadLimitedAsync(HttpContent content, long max, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > max)
            {
                return null;
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return buffer.ToArray();
    }

    [GeneratedRegex(@"^https://github\.com/(?<owner>[A-Za-z0-9-]{1,39})/(?<repo>[A-Za-z0-9._-]{1,100}?)(\.git)?/?$", RegexOptions.IgnoreCase)]
    private static partial Regex RepositoryPattern();
}
