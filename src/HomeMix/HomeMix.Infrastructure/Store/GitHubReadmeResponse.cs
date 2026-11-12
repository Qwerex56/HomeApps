using System.Text.Json.Serialization;

namespace HomeMix.Infrastructure.Store;

/// <summary>GitHub's <c>GET /repos/{owner}/{repo}/readme</c>: the file in base64.</summary>
internal sealed record GitHubReadmeResponse(
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("encoding")] string? Encoding)
{
    public string? Text => Content is not null && Encoding == "base64"
        ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Content.Replace("\n", string.Empty, StringComparison.Ordinal)))
        : null;
}
