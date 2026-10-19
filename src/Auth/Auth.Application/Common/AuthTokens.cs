using Auth.Application.Abstractions;
using Auth.Domain.Identities;

namespace Auth.Application.Common;

/// <summary>A new access token and refresh token. The Api puts the refresh token in a cookie for browser clients.</summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    ClientType ClientType);

/// <summary>What a sign-in (password, Google, Apple, link confirmation) ends with when the credentials were right.</summary>
public abstract record SignInResult
{
    private SignInResult() { }

    public sealed record SignedIn(AuthTokens Tokens) : SignInResult;

    /// <summary>A-09: the Api answers 403 <c>account.disabled_by_user</c> with this ticket.</summary>
    public sealed record ReactivationRequired(string Ticket) : SignInResult;

    /// <summary>A-15: the Api answers 409 <c>external_login.link_required</c> with this ticket.</summary>
    public sealed record LinkRequired(string Ticket, ExternalProvider Provider, string Email) : SignInResult;
}

internal static class TokenIssuerExtensions
{
    public static AuthTokens Tokens(this ITokenIssuer issuer, Identity identity, IssuedRefreshToken session, RegisteredClient client)
    {
        var access = issuer.IssueAccessToken(identity, session, client);
        return new AuthTokens(access.Value, access.ExpiresAt, session.Raw, session.ExpiresAt, client.Type);
    }

    public static SignInResult ToResult(this ITokenIssuer issuer, SignInOutcome outcome, Identity identity, RegisteredClient client) =>
        outcome switch
        {
            SignInOutcome.SignedIn signedIn => new SignInResult.SignedIn(issuer.Tokens(identity, signedIn.Token, client)),
            SignInOutcome.ReactivationRequired r => new SignInResult.ReactivationRequired(
                issuer.IssueTicket(new ReactivationTicket(r.IdentityId, r.Method))),
            _ => throw new InvalidOperationException($"Unknown sign-in outcome {outcome.GetType().Name}."),
        };
}
