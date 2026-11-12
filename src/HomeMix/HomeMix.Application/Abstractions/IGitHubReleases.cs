using FluentResults;

namespace HomeMix.Application.Abstractions;

/// <summary>Add-ons from GitHub: the asset <c>addon.zip</c> of a repository's latest (or named) release.</summary>
public interface IGitHubReleases
{
    /// <param name="repository">https://github.com/owner/repo, as a person pastes it.</param>
    /// <param name="tag">A release tag, or <c>null</c> for the latest release.</param>
    Task<Result<GitHubRelease>> DownloadAsync(string repository, string? tag, CancellationToken cancellationToken);
}
