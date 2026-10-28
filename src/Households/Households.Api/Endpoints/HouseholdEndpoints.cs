using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using Households.Api.Http;
using Households.Application.Common;
using Households.Application.Households;
using MediatR;

namespace Households.Api.Endpoints;

internal sealed record HouseholdRequest(string Name, string? Description);

/// <summary>H1–H6, the D5a banner and the guest link (D2b). The slices decide who may do what; 404 for non-members (Q3).</summary>
internal sealed class HouseholdEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var households = app.MapGroup("/households").WithTags("Households");

        households.MapPost("/", async (HouseholdRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new CreateHouseholdCommand(body.Name, body.Description), ct))
            .Map(id => new IdResponse(id))
            .ToCreatedHttpResult(created => $"/households/{created.Id}"))
            .Produces<IdResponse>(StatusCodes.Status201Created)
            .RequireRateLimiting(RateLimits.Changes);

        households.MapGet("/", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyHouseholdsQuery(), ct)).ToHttpResult())
            .Produces<IReadOnlyList<MyHouseholdView>>();

        households.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetHouseholdQuery(id), ct)).ToHttpResult())
            .Produces<HouseholdView>();

        households.MapPut("/{id:guid}", async (Guid id, HouseholdRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateHouseholdCommand(id, body.Name, body.Description), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        households.MapPost("/{id:guid}/archive", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ArchiveHouseholdCommand(id), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        households.MapPost("/{id:guid}/restore", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RestoreHouseholdCommand(id), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        households.MapPost("/{id:guid}/ownership/acknowledge", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AcknowledgeOwnershipCommand(id), ct)).ToHttpResult());

        households.MapPost("/{id:guid}/guest-link", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RegenerateGuestLinkCommand(id), ct)).Map(code => new GuestLinkResponse(code)).ToHttpResult())
            .Produces<GuestLinkResponse>()
            .RequireRateLimiting(RateLimits.Changes);

        households.MapDelete("/{id:guid}/guest-link", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DisableGuestLinkCommand(id), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);
    }
}
