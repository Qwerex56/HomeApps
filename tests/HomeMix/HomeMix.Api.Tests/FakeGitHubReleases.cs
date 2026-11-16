using System.Collections.Concurrent;
using System.Security.Cryptography;
using FluentResults;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Packages;

namespace HomeMix.Api.Tests;

/// <summary>Latest releases by repository URL, instead of GitHub: both downloading and describing them.</summary>
public sealed class FakeGitHubReleases : IGitHubReleases, IGitHubRepositories
{
    private readonly ConcurrentDictionary<string, (string Tag, byte[] Zip, string? Readme)> _releases = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>How many times a zip was downloaded, by repository.</summary>
    public ConcurrentDictionary<string, int> Downloads { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Release(string repository, string tag, byte[] zip, string? readme = null) => _releases[repository] = (tag, zip, readme);

    public Task<Result<GitHubRelease>> DownloadAsync(string repository, string? tag, CancellationToken cancellationToken)
    {
        Downloads.AddOrUpdate(repository, 1, (_, n) => n + 1);
        return Task.FromResult(_releases.TryGetValue(repository, out var release) && (tag is null || tag == release.Tag)
            ? Result.Ok(new GitHubRelease(repository, release.Tag, release.Zip, Sha(release.Zip)))
            : Result.Fail<GitHubRelease>(PackageErrors.Download("No such release.")));
    }

    public Task<GitHubLatest> LatestAsync(string repository, CancellationToken cancellationToken)
    {
        // As GitHubRepositoriesClient normalizes a pasted link.
        repository = repository.Trim().TrimEnd('/');
        repository = repository.EndsWith(".git", StringComparison.Ordinal) ? repository[..^4] : repository;
        return Task.FromResult(_releases.TryGetValue(repository, out var release)
            ? new GitHubLatest(repository, release.Tag, Sha(release.Zip), new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), release.Readme, null)
            : new GitHubLatest(repository, null, null, null, null, "no_release"));
    }

    private static string Sha(byte[] zip) => Convert.ToHexStringLower(SHA256.HashData(zip));
}
