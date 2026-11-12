using System.Text.Json.Serialization;

namespace HomeMix.Infrastructure.GitHub;

/// <summary>A release asset as GitHub's REST API returns it; <c>digest</c> is <c>sha256:&lt;hex&gt;</c>.</summary>
internal sealed record GitHubAsset(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("size")] long Size,
    [property: JsonPropertyName("digest")] string? Digest,
    [property: JsonPropertyName("browser_download_url")] string DownloadUrl);
