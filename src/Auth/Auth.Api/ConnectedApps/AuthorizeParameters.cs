using Auth.Application.ConnectedApps;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.ConnectedApps;

/// <summary>The OAuth authorization request's query, as the consent screen sends it back unchanged.</summary>
internal sealed record AuthorizeParameters(
    [property: FromQuery(Name = "response_type")] string? ResponseType,
    [property: FromQuery(Name = "client_id")] string? ClientId,
    [property: FromQuery(Name = "redirect_uri")] string? RedirectUri,
    [property: FromQuery(Name = "code_challenge")] string? CodeChallenge,
    [property: FromQuery(Name = "code_challenge_method")] string? CodeChallengeMethod,
    [property: FromQuery(Name = "resource")] string? Resource,
    [property: FromQuery(Name = "state")] string? State)
{
    public AuthorizationRequest ToRequest() =>
        new(ResponseType, ClientId, RedirectUri, CodeChallenge, CodeChallengeMethod, Resource, State);
}
