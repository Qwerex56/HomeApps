namespace HomeMix.Api.Requests;

/// <param name="Repository">https://github.com/owner/repo</param>
/// <param name="Tag">A release tag; the latest release when left out.</param>
public sealed record GitHubPackageRequest(string Repository, string? Tag);
