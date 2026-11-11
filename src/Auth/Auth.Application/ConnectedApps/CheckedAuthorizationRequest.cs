using Auth.Domain.ConnectedApps;

namespace Auth.Application.ConnectedApps;

/// <summary>An authorization request that passed every check: the app, where the code goes and for which resource.</summary>
internal sealed record CheckedAuthorizationRequest(AppClient Client, string RedirectUri, string CodeChallenge, string Audience, string? State);
