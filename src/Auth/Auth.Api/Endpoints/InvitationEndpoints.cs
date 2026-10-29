using Auth.Api.Http;
using Auth.Application.Invitations;
using Auth.Domain.Invitations;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Auth.Api.Endpoints;

internal sealed record CreateInvitationRequest(string Email);

internal sealed record AcceptInvitationRequest(
    string Password, string DisplayName, string? FirstName, string? LastName, string? Locale, string ClientId);

/// <summary>A-07, A-20, A-21. Who may see or change which invitation is decided in the slices (R10).</summary>
internal sealed class InvitationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var invitations = app.MapGroup("/auth/invitations").WithTags("Invitations");

        invitations.MapPost("/", async (CreateInvitationRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreateInvitationCommand(body.Email), ct))
                .ToCreatedHttpResult(_ => "/auth/invitations"))
            .RequireRateLimiting(RateLimits.Mail);

        invitations.MapGet("/", async (bool? all, InvitationStatus? status, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListInvitationsQuery(all ?? false, status), ct)).ToHttpResult());

        invitations.MapPost("/{id:guid}/resend", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ResendInvitationCommand(id), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Mail);

        invitations.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RevokeInvitationCommand(id), ct)).ToHttpResult());

        invitations.MapGet("/{token}", async (string token, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetInvitationPreviewQuery(token), ct)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);

        invitations.MapPost("/{token}/accept", async (
                string token, AcceptInvitationRequest body, HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AcceptInvitationWithPasswordCommand(
                token, body.Password, body.DisplayName, body.FirstName, body.LastName, body.Locale, body.ClientId,
                http.UserAgent()), ct)).ToTokenResult(http))
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Anonymous);
    }
}
