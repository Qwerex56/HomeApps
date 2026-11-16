namespace HomeMix.Application.Abstractions;

/// <summary>What GitHub says about a repository's latest release (two calls: the release and the README).</summary>
public interface IGitHubRepositories
{
    Task<GitHubLatest> LatestAsync(string repository, CancellationToken cancellationToken);
}
