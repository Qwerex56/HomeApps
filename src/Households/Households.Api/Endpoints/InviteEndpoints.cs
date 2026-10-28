using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using Households.Api.Http;
using Households.Application.Common;
using Households.Application.Invites;
using Households.Domain.Households;
using Households.Domain.Invites;
using MediatR;

namespace Households.Api.Endpoints;

internal sealed record InviteRequest(HouseholdRole Role, InviteValidity? Validity, DateTimeOffset? GuestAccessUntil);

/// <summary>
/// I1–I3 inside a household, I4–I5 by code. The code is either a personal invite or the household's guest link; both
/// open the same front route <c>/invite/{code}</c>, typed with or without the dash.
/// </summary>
internal sealed class InviteEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var invites = app.MapGroup("/households/{id:guid}/invites").WithTags("Invites");

        invites.MapPost("/", async (Guid id, InviteRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreateInviteCommand(id, body.Role, body.Validity ?? InviteValidity.SevenDays, body.GuestAccessUntil), ct))
            .ToCreatedHttpResult(invite => $"/households/{id}/invites/{invite.Id}"))
            .Produces<InviteView>(StatusCodes.Status201Created)
            .RequireRateLimiting(RateLimits.Changes);

        invites.MapGet("/", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListInvitesQuery(id), ct)).ToHttpResult())
            .Produces<IReadOnlyList<InviteView>>();

        invites.MapDelete("/{inviteId:guid}", async (Guid id, Guid inviteId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RevokeInviteCommand(id, inviteId), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        var byCode = app.MapGroup("/invites/{code}").WithTags("Invites");

        byCode.MapGet("/", async (string code, ISender sender, CancellationToken ct) =>
            (await sender.Send(new PreviewInviteQuery(code), ct)).ToHttpResult())
            .Produces<InvitePreviewView>()
            .RequireRateLimiting(RateLimits.Invites);

        byCode.MapPost("/accept", async (string code, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AcceptInviteCommand(code), ct)).Map(id => new IdResponse(id)).ToHttpResult())
            .Produces<IdResponse>()
            .RequireRateLimiting(RateLimits.Invites);
    }
}
