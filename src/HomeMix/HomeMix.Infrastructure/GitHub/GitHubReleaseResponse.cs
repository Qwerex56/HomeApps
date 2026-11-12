using System.Text.Json.Serialization;

namespace HomeMix.Infrastructure.GitHub;

internal sealed record GitHubReleaseResponse(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("assets")] IReadOnlyList<GitHubAsset> Assets);
