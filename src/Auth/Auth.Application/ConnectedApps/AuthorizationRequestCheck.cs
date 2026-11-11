using Auth.Application.Abstractions;
using Auth.Domain.ConnectedApps;
using FluentResults;

namespace Auth.Application.ConnectedApps;

/// <summary>The same checks for the consent screen's preview and for the decision.</summary>
internal static class AuthorizationRequestCheck
{
    public static async Task<Result<CheckedAuthorizationRequest>> CheckAsync(
        this IAppClientRepository clients, AuthorizationRequest request, ConnectedAppsOptions options, CancellationToken cancellationToken)
    {
        var client = string.IsNullOrWhiteSpace(request.ClientId)
            ? null
            : await clients.GetByIdAsync(request.ClientId, cancellationToken);
        if (client is null)
        {
            return Result.Fail<CheckedAuthorizationRequest>(ConnectedAppErrors.InvalidClient);
        }

        if (!client.AllowsRedirect(request.RedirectUri))
        {
            return Result.Fail<CheckedAuthorizationRequest>(
                ConnectedAppErrors.InvalidRedirectUri("The redirect URI is not registered for this app."));
        }

        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal))
        {
            return Result.Fail<CheckedAuthorizationRequest>(ConnectedAppErrors.InvalidRequest("response_type must be code."));
        }

        if (!string.Equals(request.CodeChallengeMethod, Pkce.Method, StringComparison.Ordinal)
            || !Pkce.IsValidChallenge(request.CodeChallenge))
        {
            return Result.Fail<CheckedAuthorizationRequest>(ConnectedAppErrors.InvalidRequest("PKCE with S256 is required."));
        }

        var audience = options.FindResource(request.Resource);
        if (audience is null)
        {
            return Result.Fail<CheckedAuthorizationRequest>(ConnectedAppErrors.InvalidTarget);
        }

        return new CheckedAuthorizationRequest(client, request.RedirectUri!, request.CodeChallenge!, audience, request.State);
    }
}
