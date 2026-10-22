using Auth.Application.Abstractions;
using Auth.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Api.Results;

namespace Auth.Api.Http;

/// <summary>What every sign-in answers: the access token in the body, the refresh token in the cookie or the body.</summary>
internal sealed record TokenResponse(
    string AccessToken, DateTimeOffset AccessTokenExpiresAt, string? RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

internal static class TokenResults
{
    public const string TicketExtension = "ticket";
    public const string DisabledByUser = "account.disabled_by_user";
    public const string LinkRequired = "external_login.link_required";

    public static IResult Tokens(HttpContext http, AuthTokens tokens)
    {
        if (tokens.ClientType == ClientType.Browser)
        {
            RefreshCookie.Set(http, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
            return TypedResults.Ok(new TokenResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt, null, tokens.RefreshTokenExpiresAt));
        }

        return TypedResults.Ok(new TokenResponse(
            tokens.AccessToken, tokens.AccessTokenExpiresAt, tokens.RefreshToken, tokens.RefreshTokenExpiresAt));
    }

    public static IResult ToTokenResult(this Result<AuthTokens> result, HttpContext http) =>
        result.IsSuccess ? Tokens(http, result.Value) : ResultHttpMapper.ToProblem(result.Errors);

    /// <summary>A-09 answers 403 with a reactivation ticket, A-15 answers 409 with a link ticket.</summary>
    public static IResult ToSignInResult(this Result<SignInResult> result, HttpContext http)
    {
        if (result.IsFailed)
        {
            return ResultHttpMapper.ToProblem(result.Errors);
        }

        return result.Value switch
        {
            SignInResult.SignedIn signedIn => Tokens(http, signedIn.Tokens),
            SignInResult.ReactivationRequired reactivation => Results.Problem(
                detail: "You disabled this account. Confirm that you want it back.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Request could not be processed",
                extensions: new Dictionary<string, object?>
                {
                    [ResultHttpMapper.CodeExtension] = DisabledByUser,
                    [TicketExtension] = reactivation.Ticket,
                }),
            SignInResult.LinkRequired link => Results.Problem(
                detail: "An account with this email already exists. Sign in to it to link the new login.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Request could not be processed",
                extensions: new Dictionary<string, object?>
                {
                    [ResultHttpMapper.CodeExtension] = LinkRequired,
                    [TicketExtension] = link.Ticket,
                    ["provider"] = link.Provider.ToString(),
                    ["email"] = link.Email,
                }),
            _ => throw new InvalidOperationException($"Unknown sign-in result {result.Value.GetType().Name}."),
        };
    }

    /// <summary>202: the change waits for the link from the email (P8).</summary>
    public static IResult ToAcceptedResult(this Result result) =>
        result.IsSuccess ? Results.Accepted() : ResultHttpMapper.ToProblem(result.Errors);

    public static string? UserAgent(this HttpContext http)
    {
        var value = http.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
