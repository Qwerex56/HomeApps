namespace HomeMix.Application.Abstractions;

/// <summary>
/// One add-on from the store's index as GitHub describes it now: the repository, its latest release with the SHA-256
/// of <c>addon.zip</c>, and the README. <paramref name="Problem"/> says why a listed repository cannot be installed.
/// </summary>
public sealed record StoreListing(
    string Repository,
    string Title,
    string? Description,
    string? Version,
    string? Sha256,
    string? Readme,
    DateTimeOffset? PublishedAt,
    string? Problem);
