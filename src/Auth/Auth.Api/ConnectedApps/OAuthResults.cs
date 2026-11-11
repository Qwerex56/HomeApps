using Auth.Application.ConnectedApps;
using Auth.Domain.ConnectedApps;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Auth.Api.ConnectedApps;

/// <summary>Answers of the endpoints OAuth clients call (token, register, revoke) in OAuth's own shape.</summary>
internal static class OAuthResults
{
    public static IResult Tokens(HttpContext http, Result<AppTokens> result)
    {
        NoStore(http);
        if (result.IsFailed)
        {
            return Error(result.Errors, "invalid_request");
        }

        var tokens = result.Value;
        return TypedResults.Ok(new OAuthTokenResponse(tokens.AccessToken, "Bearer", tokens.ExpiresIn, tokens.RefreshToken, tokens.Scope));
    }

    /// <summary>
    /// The OAuth <c>error</c> comes from an <see cref="ConnectedAppErrors"/> code; anything else (a failed validator) is
    /// <paramref name="fallback"/>. <c>invalid_client</c> is 401 (RFC 6749 5.2), everything else 400.
    /// </summary>
    public static IResult Error(IReadOnlyList<IError> errors, string fallback)
    {
        var oauth = errors.OfType<AppError>().FirstOrDefault(e => e.Code.StartsWith(ConnectedAppErrors.Prefix, StringComparison.Ordinal));
        var code = oauth?.Code[ConnectedAppErrors.Prefix.Length..] ?? fallback;
        var description = oauth?.Message ?? (errors.Count > 0 ? errors[0].Message : null);
        var status = code == "invalid_client" ? StatusCodes.Status401Unauthorized : StatusCodes.Status400BadRequest;
        return TypedResults.Json(new OAuthErrorResponse(code, description), statusCode: status);
    }

    /// <summary>RFC 6749 5.1: token responses are never cached.</summary>
    public static void NoStore(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        http.Response.Headers.Pragma = "no-cache";
    }
}
