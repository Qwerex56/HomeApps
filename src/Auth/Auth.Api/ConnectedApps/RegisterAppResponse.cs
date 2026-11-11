using System.Text.Json.Serialization;

namespace Auth.Api.ConnectedApps;

/// <summary>RFC 7591 3.2.1: the registered metadata and the new <c>client_id</c>, never a secret.</summary>
internal sealed record RegisterAppResponse(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("client_name")] string ClientName,
    [property: JsonPropertyName("redirect_uris")] IReadOnlyList<string> RedirectUris,
    [property: JsonPropertyName("client_id_issued_at")] long ClientIdIssuedAt)
{
    [JsonPropertyName("grant_types")]
    public IReadOnlyList<string> GrantTypes { get; } = ["authorization_code", "refresh_token"];

    [JsonPropertyName("response_types")]
    public IReadOnlyList<string> ResponseTypes { get; } = ["code"];

    [JsonPropertyName("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; } = "none";
}
