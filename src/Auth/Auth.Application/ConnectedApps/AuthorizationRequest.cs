namespace Auth.Application.ConnectedApps;

/// <summary>The query of <c>/auth/oauth/authorize</c> (OAuth 2.1 4.1.1, RFC 8707), as the consent screen passes it on.</summary>
public sealed record AuthorizationRequest(
    string? ResponseType,
    string? ClientId,
    string? RedirectUri,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? Resource,
    string? State);
