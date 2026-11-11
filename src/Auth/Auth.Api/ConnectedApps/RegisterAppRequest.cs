using System.Text.Json.Serialization;

namespace Auth.Api.ConnectedApps;

/// <summary>RFC 7591 client metadata; only what this server uses, the rest is ignored.</summary>
internal sealed record RegisterAppRequest(
    [property: JsonPropertyName("client_name")] string? ClientName,
    [property: JsonPropertyName("redirect_uris")] IReadOnlyList<string>? RedirectUris,
    [property: JsonPropertyName("grant_types")] IReadOnlyList<string>? GrantTypes,
    [property: JsonPropertyName("response_types")] IReadOnlyList<string>? ResponseTypes,
    [property: JsonPropertyName("token_endpoint_auth_method")] string? TokenEndpointAuthMethod);
