using Auth.Application.ConnectedApps;

namespace Auth.Api.ConnectedApps;

/// <summary>The consent screen's answer: the authorization request it was opened with, and the person's choice.</summary>
internal sealed record ConsentDecisionRequest(
    string? ResponseType,
    string? ClientId,
    string? RedirectUri,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? Resource,
    string? State,
    bool Allow)
{
    public AuthorizationRequest ToRequest() =>
        new(ResponseType, ClientId, RedirectUri, CodeChallenge, CodeChallengeMethod, Resource, State);
}
