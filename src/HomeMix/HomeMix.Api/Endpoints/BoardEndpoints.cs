using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Api.Requests;
using HomeMix.Application.Boards;
using HomeMix.Application.Boards.GetBoard;
using HomeMix.Application.Boards.SaveBoard;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary>
/// UI 2.4.1: where a household's widgets stand on its page (<c>Household</c>) and its wall tablet (<c>Wall</c>), on an
/// 8-column grid. Every member reads; admins and up arrange. An assistant (JARVIS) only reads (the kit's default).
/// </summary>
internal sealed class BoardEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var api = app.MapVersionedGroup("/homemix/api").MapGroup("/households/{householdId:guid}/boards").WithTags("Boards");

        api.MapGet("/{place}", async (Guid householdId, string place, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetBoardQuery(householdId, place), ct)).ToHttpResult())
            .Produces<BoardView>();

        api.MapPut("/{place}", async (Guid householdId, string place, SaveBoardRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SaveBoardCommand(householdId, place, body.Items ?? []), ct)).ToHttpResult())
            .Produces<BoardView>();
    }
}
