using HouseholdService.Application.Features.Invite.Commands.CreateInvite;
using HouseholdService.Application.Features.Invite.Queries.GetAllInvites;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HouseholdService.Presentation.Endpoints;

public static class InviteEndpoints {
    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder builder) {
        var group = builder.MapGroup("invite");

        group.MapGet("/get-invites", GetInvitesInHousehold);
        group.MapPost("create-invite", CreateInvite);

        return builder;
    }

    private static async Task<IResult> GetInvitesInHousehold(Guid householdId, Guid userId, [FromServices] IMediator mediator) {
        var invites = await mediator.Send(new GetAllInvitesQuery(userId, householdId));

        return Results.Ok(invites);
    }

    private static async Task<IResult> CreateInvite(Guid userId, Guid householdId, [FromServices] IMediator mediator) {
        var result = await mediator.Send(new CreateInviteCommand(householdId, userId));

        return result.IsFailed
            ? TypedResults.BadRequest(result.Errors)
            : Results.Ok();
    }
}