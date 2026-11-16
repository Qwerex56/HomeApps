namespace HomeMix.Application.Abstractions;

/// <summary>
/// A repository's latest release as GitHub describes it, without downloading: the SHA-256 GitHub gives for
/// <c>addon.zip</c>, and the README. <paramref name="Problem"/> (<c>invalid_link</c>, <c>no_release</c>,
/// <c>no_addon_zip</c>, <c>no_digest</c>, <c>unavailable</c>) says why it cannot be installed.
/// </summary>
/// <param name="Repository">https://github.com/owner/repo as normalized from the link; null for a link that is not one.</param>
public sealed record GitHubLatest(
    string? Repository, string? Tag, string? Sha256, DateTimeOffset? PublishedAt, string? Readme, string? Problem);
