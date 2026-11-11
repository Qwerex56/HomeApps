using Auth.Application.Abstractions;
using Auth.Domain.Identities;

namespace Auth.Application.ConnectedApps;

internal static class AppTokensFactory
{
    public static AppTokens IssueAppTokens(
        this ITokenIssuer issuer, Identity identity, IssuedRefreshToken session, string clientId, DateTimeOffset now)
    {
        var access = issuer.IssueAppAccessToken(identity, session, clientId);
        var expiresIn = (int)Math.Max(1, Math.Round((access.ExpiresAt - now).TotalSeconds));
        return new AppTokens(access.Value, expiresIn, session.Raw, AppTokens.HomeAppsScope);
    }
}
