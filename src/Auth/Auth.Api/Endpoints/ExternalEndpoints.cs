using Auth.Api.Http;
using Auth.Application.Common;
using Auth.Application.External;
using FluentResults;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Domain.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Auth.Application.Common.SignInResult;

namespace Auth.Api.Endpoints;

internal sealed record LinkConfirmRequest(string Ticket, string Password, string ClientId);

internal sealed record LinkStartRequest(string ClientId, string? ReturnUrl);

internal sealed record LinkStartResponse(Uri Url);

/// <summary>
/// A-05, A-07 (b), A-11, A-15: Google. The callback always ends in a redirect to Vue; tickets and error codes travel in
/// the URL fragment, which never reaches a server log (Q14).
/// </summary>
internal sealed class ExternalEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var external = app.MapGroup("/auth/external").WithTags("Google");

        external.MapGet("/{provider}", async (
                string provider,
                [FromQuery] string? returnUrl,
                [FromQuery] string? invitation,
                [FromQuery(Name = "client_id")] string? clientId,
                [FromQuery] string? locale,
                HttpContext http,
                ExternalFlows flows,
                CancellationToken ct) =>
            {
                if (!ExternalFlows.TryParseProvider(provider, out var parsed))
                {
                    return Results.NotFound();
                }

                var started = await flows.StartAsync(http, parsed, clientId, returnUrl, invitation, locale, null, ct);
                return started.IsSuccess ? Results.Redirect(started.Value.ToString()) : ResultHttpMapper.ToProblem(started.Errors);
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);

        external.MapGet("/{provider}/callback", async (
                string provider,
                [FromQuery] string? code,
                [FromQuery] string? state,
                [FromQuery] string? error,
                HttpContext http,
                ExternalFlows flows,
                ISender sender,
                CancellationToken ct) =>
            {
                var flow = flows.Complete(http, state);
                if (flow is null || !ExternalFlows.TryParseProvider(provider, out var parsed) || parsed != flow.Provider)
                {
                    return ResultHttpMapper.ToProblem([ApplicationErrors.ExternalLoginFailed]);
                }

                if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
                {
                    return BackToApp(flow.ReturnUrl, ("error", "external_login.cancelled"));
                }

                var redirectUri = flows.CallbackUri(flow.Provider);
                if (flow.LinkIdentityId is { } identityId)
                {
                    var linked = await sender.Send(
                        new LinkExternalLoginCommand(identityId, flow.Provider, code, flow.CodeVerifier, redirectUri), ct);
                    return linked.IsSuccess
                        ? BackToApp(flow.ReturnUrl, ("linked", ExternalFlows.ProviderSegment(flow.Provider)))
                        : BackToApp(flow.ReturnUrl, ("error", ErrorCode(linked.Errors)));
                }

                var result = await sender.Send(new SignInExternalCommand(
                    flow.Provider, code, flow.CodeVerifier, redirectUri, flow.Invitation, flow.Locale, flow.ClientId,
                    http.UserAgent()), ct);
                if (result.IsFailed)
                {
                    return BackToApp(flow.ReturnUrl, ("error", ErrorCode(result.Errors)));
                }

                switch (result.Value)
                {
                    case SignInResult.SignedIn signedIn:
                        // Only browser clients start this flow; Vue gets its access token from /auth/refresh.
                        RefreshCookie.Set(http, signedIn.Tokens.RefreshToken, signedIn.Tokens.RefreshTokenExpiresAt);
                        return BackToApp(flow.ReturnUrl);
                    case SignInResult.ReactivationRequired reactivation:
                        return BackToApp(flow.ReturnUrl, ("error", TokenResults.DisabledByUser), ("ticket", reactivation.Ticket));
                    case SignInResult.LinkRequired link:
                        return BackToApp(
                            flow.ReturnUrl,
                            ("error", TokenResults.LinkRequired),
                            ("ticket", link.Ticket),
                            ("provider", ExternalFlows.ProviderSegment(link.Provider)),
                            ("email", link.Email));
                    default:
                        throw new InvalidOperationException($"Unknown sign-in result {result.Value.GetType().Name}.");
                }
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);

        external.MapPost("/link/confirm", async (LinkConfirmRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ConfirmExternalLinkCommand(body.Ticket, body.Password, body.ClientId, http.UserAgent()), ct))
                .ToSignInResult(http))
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);

        // A-15 "No": the ticket simply expires; nothing was created, so there is nothing to undo.
        external.MapPost("/link/decline", () => Results.NoContent())
            .AllowAnonymous();

        // A-11 (Q15): a navigation cannot carry the access token, so Vue asks for the URL first and then goes there.
        app.MapPost("/auth/me/logins/{provider}/link", async (
                string provider,
                LinkStartRequest body,
                HttpContext http,
                ICurrentUser currentUser,
                ExternalFlows flows,
                CancellationToken ct) =>
            {
                if (!ExternalFlows.TryParseProvider(provider, out var parsed))
                {
                    return Results.NotFound();
                }

                var started = await flows.StartAsync(
                    http, parsed, body.ClientId, body.ReturnUrl, null, null, currentUser.RequiredUserId, ct);
                return started.IsSuccess
                    ? Results.Ok(new LinkStartResponse(started.Value))
                    : ResultHttpMapper.ToProblem(started.Errors);
            })
            .WithTags("My account");
    }

    private static IResult BackToApp(Uri returnUrl, params (string Key, string Value)[] fragment)
    {
        var target = new UriBuilder(returnUrl)
        {
            Fragment = string.Join('&', fragment.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}")),
        };
        return Results.Redirect(target.Uri.ToString());
    }

    private static string ErrorCode(IReadOnlyList<IError> errors) =>
        errors.OfType<AppError>().FirstOrDefault()?.Code ?? "external_login.failed";
}
