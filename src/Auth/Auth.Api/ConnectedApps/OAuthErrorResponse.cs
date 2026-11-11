using System.Text.Json.Serialization;

namespace Auth.Api.ConnectedApps;

/// <summary>RFC 6749 5.2 and RFC 7591 3.2.2: OAuth clients read this shape, not problem details.</summary>
internal sealed record OAuthErrorResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("error_description")] string? ErrorDescription);
