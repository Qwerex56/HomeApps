using Auth.Api.Http;
using Auth.Application.MyAccount;
using Auth.Application.Password;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Auth.Api.Endpoints;

internal sealed record ChangePasswordRequest(string? CurrentPassword, string NewPassword);

internal sealed record ChangeEmailRequest(string NewEmail, string? CurrentPassword);

internal sealed record ConfirmTokenRequest(string Token);

/// <summary>
/// A-10 to A-14: the signed-in user's own login data. The confirmations come from links in emails, which may be
/// opened in another browser, so they need only the token from the link (P8).
/// </summary>
internal sealed class MeEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/auth/me").WithTags("My account");

        me.MapGet("/", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMeQuery(), ct)).ToHttpResult());

        me.MapPut("/password", async (ChangePasswordRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RequestPasswordChangeCommand(body.CurrentPassword, body.NewPassword), ct)).ToAcceptedResult())
            .RequireRateLimiting(RateLimits.Mail);

        me.MapPost("/password/confirm", async (ConfirmTokenRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ConfirmPasswordChangeCommand(body.Token), ct)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);

        me.MapPut("/email", async (ChangeEmailRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RequestEmailChangeCommand(body.NewEmail, body.CurrentPassword), ct)).ToAcceptedResult())
            .RequireRateLimiting(RateLimits.Mail);

        me.MapPost("/email/confirm", async (ConfirmTokenRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ConfirmEmailChangeCommand(body.Token), ct)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);

        me.MapGet("/sessions", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListSessionsQuery(), ct)).ToHttpResult());

        me.MapDelete("/sessions/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RevokeSessionCommand(id), ct)).ToHttpResult());

        me.MapDelete("/logins/{provider}", async (string provider, ISender sender, CancellationToken ct) =>
            ExternalFlows.TryParseProvider(provider, out var parsed)
                ? (await sender.Send(new UnlinkExternalLoginCommand(parsed), ct)).ToHttpResult()
                : Results.NotFound());
    }
}
