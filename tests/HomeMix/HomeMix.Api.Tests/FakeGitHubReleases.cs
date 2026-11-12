using System.Collections.Concurrent;
using System.Security.Cryptography;
using FluentResults;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Packages;

namespace HomeMix.Api.Tests;

/// <summary>Latest releases by repository URL, instead of GitHub.</summary>
public sealed class FakeGitHubReleases : IGitHubReleases
{
    private readonly ConcurrentDictionary<string, (string Tag, byte[] Zip)> _releases = new(StringComparer.OrdinalIgnoreCase);

    public void Release(string repository, string tag, byte[] zip) => _releases[repository] = (tag, zip);

    public Task<Result<GitHubRelease>> DownloadAsync(string repository, string? tag, CancellationToken cancellationToken) =>
        Task.FromResult(_releases.TryGetValue(repository, out var release) && (tag is null || tag == release.Tag)
            ? Result.Ok(new GitHubRelease(repository, release.Tag, release.Zip, Convert.ToHexStringLower(SHA256.HashData(release.Zip))))
            : Result.Fail<GitHubRelease>(PackageErrors.Download("No such release.")));
}
