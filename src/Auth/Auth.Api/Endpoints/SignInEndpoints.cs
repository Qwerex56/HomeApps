using Auth.Api.Http;
using Auth.Application.Login;
using Auth.Application.Logout;
using Auth.Application.Reactivation;
using Auth.Application.Refresh;
using Auth.Application.Register;
using Auth.Domain.Identities;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Domain.Errors;
using MediatR;

namespace Auth.Api.Endpoints;

internal sealed record RegisterRequest(
    string Email, string Password, string DisplayName, string? FirstName, string? LastName, string? Locale, string ClientId);

internal sealed record LoginRequest(string Email, string Password, string ClientId);

internal sealed record RefreshRequest(string ClientId, string? RefreshToken);

internal sealed record LogoutRequest(string? RefreshToken);

internal sealed record ReactivationRequest(string Ticket, string ClientId);

/// <summary>A-01, A-02, A-03, A-04, A-09, A-14: getting tokens and giving them back.</summary>
internal sealed class SignInEndpoints : IEndpoint
{
    /// <summary>Q5: how long Vue waits before asking again while Accounts enables the account.</summary>
    private const string ReactivationRetryAfterSeconds = "2";
    private const string ReactivationPending = "reactivation.pending";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Sign-in");

        auth.MapPost("/register", async (RegisterRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RegisterWithPasswordCommand(
                body.Email, body.Password, body.DisplayName, body.FirstName, body.LastName, body.Locale, body.ClientId,
                http.UserAgent()), ct)).ToTokenResult(http))
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);

        auth.MapPost("/login", async (LoginRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SignInWithPasswordCommand(body.Email, body.Password, body.ClientId, http.UserAgent()), ct))
                .ToSignInResult(http))
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);

        // The JSON body is required on purpose: a cross-site form cannot send it without a CORS preflight.
        auth.MapPost("/refresh", async (RefreshRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var token = RefreshCookie.Read(http) ?? body.RefreshToken;
                if (string.IsNullOrEmpty(token))
                {
                    return ResultHttpMapper.ToProblem([IdentityErrors.InvalidRefreshToken]);
                }

                // A failed refresh keeps the cookie: the loser of two tabs refreshing at once must not log the winner out (Q8).
                return (await sender.Send(new RefreshTokensCommand(token, body.ClientId, http.UserAgent()), ct)).ToTokenResult(http);
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Refresh);

        auth.MapPost("/logout", async (LogoutRequest? body, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var token = RefreshCookie.Read(http) ?? body?.RefreshToken;
                RefreshCookie.Delete(http);
                if (!string.IsNullOrEmpty(token))
                {
                    await sender.Send(new SignOutCommand(token), ct);
                }

                return Results.NoContent();
            })
            .AllowAnonymous();

        auth.MapPost("/logout-all", async (HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SignOutEverywhereCommand(), ct);
                if (result.IsSuccess)
                {
                    RefreshCookie.Delete(http);
                }

                return result.ToHttpResult();
            });

        auth.MapPost("/reactivation/complete", async (ReactivationRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CompleteReactivationCommand(body.Ticket, body.ClientId, http.UserAgent()), ct);
                if (result.HasError<AppError>(e => e.Code == ReactivationPending))
                {
                    http.Response.Headers.RetryAfter = ReactivationRetryAfterSeconds;
                }

                return result.ToTokenResult(http);
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);
    }
}
