using System.Text.Json.Serialization;

namespace HomeMix.Infrastructure.Store;

/// <summary>The part of GitHub's <c>GET /repos/{owner}/{repo}</c> the store shows.</summary>
internal sealed record GitHubRepositoryResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description);
