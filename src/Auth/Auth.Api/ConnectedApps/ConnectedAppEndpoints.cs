using Auth.Api.Http;
using Auth.Application.ConnectedApps.DecideConsent;
using Auth.Application.ConnectedApps.ListConnectedApps;
using Auth.Application.ConnectedApps.PreviewConsent;
using Auth.Application.ConnectedApps.RedeemCode;
using Auth.Application.ConnectedApps.RefreshAppTokens;
using Auth.Application.ConnectedApps.RegisterApp;
using Auth.Application.ConnectedApps.RevokeAppToken;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Auth.Api.ConnectedApps;

/// <summary>
/// OAuth 2.1 for apps that act for a person, e.g. Claude through the JARVIS gateway (jarvis-v1.md 3.1). The person
/// signs in and decides in Vue (<c>/connect</c>); the app gets tokens for one resource only, never for the services.
/// </summary>
internal sealed class ConnectedAppEndpoints : IEndpoint
{
    /// <summary>The consent screen in Vue. Same host as Auth behind Traefik, so a relative redirect is enough.</summary>
    public const string ConsentPage = "/connect";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var oauth = app.MapGroup("/auth/oauth").WithTags("Connected apps");

        // OAuth 2.1 4.1.1: the browser arrives here from the app. Everything is checked by the consent screen's
        // preview, which shows the error instead of sending the browser to an address nobody registered.
        oauth.MapGet("/authorize", (HttpContext http) => Results.Redirect(ConsentPage + http.Request.QueryString))
            .AllowAnonymous();

        oauth.MapPost("/register", async (RegisterAppRequest body, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RegisterAppCommand(
                    body.ClientName, body.RedirectUris, body.GrantTypes, body.ResponseTypes, body.TokenEndpointAuthMethod), ct);
                if (result.IsFailed)
                {
                    return OAuthResults.Error(result.Errors, "invalid_client_metadata");
                }

                var registered = result.Value;
                return TypedResults.Json(
                    new RegisterAppResponse(registered.ClientId, registered.ClientName, registered.RedirectUris, registered.IssuedAt.ToUnixTimeSeconds()),
                    statusCode: StatusCodes.Status201Created);
            })
            .AllowAnonymous()
            .RequireCors(OAuthCors.Policy)
            .RequireRateLimiting(RateLimits.Anonymous);

        // Form-encoded like every OAuth token endpoint (RFC 6749 3.2); read by hand, there is no antiforgery to skip.
        oauth.MapPost("/token", async (HttpContext http, ISender sender, CancellationToken ct) =>
            {
                if (!http.Request.HasFormContentType)
                {
                    OAuthResults.NoStore(http);
                    return TypedResults.Json(
                        new OAuthErrorResponse("invalid_request", "Send the request as application/x-www-form-urlencoded."),
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var form = await http.Request.ReadFormAsync(ct);
                string? Field(string name) => form.TryGetValue(name, out var value) ? value.ToString() : null;

                return Field("grant_type") switch
                {
                    "authorization_code" => OAuthResults.Tokens(http, await sender.Send(new RedeemCodeCommand(
                        Field("client_id"), Field("code"), Field("redirect_uri"), Field("code_verifier"), Field("resource"),
                        http.UserAgent()), ct)),
                    "refresh_token" => OAuthResults.Tokens(http, await sender.Send(new RefreshAppTokensCommand(
                        Field("client_id"), Field("refresh_token"), Field("resource"), http.UserAgent()), ct)),
                    _ => OAuthResults.Error([Domain.ConnectedApps.ConnectedAppErrors.UnsupportedGrantType], "unsupported_grant_type"),
                };
            })
            .AllowAnonymous()
            .RequireCors(OAuthCors.Policy)
            .RequireRateLimiting(RateLimits.Refresh);

        // RFC 7009: 200 whatever the token was.
        oauth.MapPost("/revoke", async (HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var form = http.Request.HasFormContentType ? await http.Request.ReadFormAsync(ct) : null;
                await sender.Send(new RevokeAppTokenCommand(form?["token"].ToString(), form?["client_id"].ToString() is { Length: > 0 } c ? c : null), ct);
                return Results.Ok();
            })
            .AllowAnonymous()
            .RequireCors(OAuthCors.Policy)
            .RequireRateLimiting(RateLimits.Refresh);

        // The consent screen (signed in): what to show, then the decision. An assistant's token cannot decide (guard).
        oauth.MapGet("/consent", async ([AsParameters] AuthorizeParameters query, ISender sender, CancellationToken ct) =>
            (await sender.Send(new PreviewConsentQuery(query.ToRequest()), ct)).ToHttpResult());

        oauth.MapPost("/consent", async (ConsentDecisionRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DecideConsentCommand(body.ToRequest(), body.Allow), ct)).ToHttpResult());

        // "Connected apps" in the account settings; disconnecting is DELETE /auth/me/sessions/{id}.
        app.MapGet("/auth/me/apps", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListConnectedAppsQuery(), ct)).ToHttpResult())
            .WithTags("My account");
    }
}
