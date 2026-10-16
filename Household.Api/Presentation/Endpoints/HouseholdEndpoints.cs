using System.Security.Claims;
using HouseholdService.Application.Features.Household.Commands.AddMember;
using HouseholdService.Application.Features.Household.Commands.CreateHousehold;
using HouseholdService.Application.Features.Household.Commands.RemoveMember;
using HouseholdService.Application.Features.Household.Commands.UpdateHousehold;
using HouseholdService.Application.Features.Household.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HouseholdService.Presentation.Endpoints;

public static class HouseholdEndpoints {
    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder builder) {
        var group = builder.MapGroup("household");

        // GET
        group.MapGet("{userId:guid}/page={page:int:min(0)}/page-size={pageSize:int:range(5,20)}",
                     GetUserHouseholds); // Fetch all user households
        group.MapGet("{userId:guid}/{householdId:guid}", GetHousehold); // Fetch one household
        group.MapGet("{householdId:guid}/members", GetHouseholdUserList);

        // POST
        group.MapPost("create", CreateHousehold);
        group.MapPost("{householdId:guid}/add-member/{inviteeId:guid}", AddMemberToHousehold);

        // PUT
        group.MapPut("{householdId:guid}/update-household", UpdateHousehold);

        // DELETE
        group.MapDelete("{householdId:guid}/remove-member/{memberToRemoveId:guid}", RemoveMemberFromHousehold);

        return builder;
    }

    private static async Task<IResult> CreateHousehold(HttpContext context,
                                                       string name,
                                                       string description,
                                                       [FromServices] IMediator mediator) {
        var userGuidString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userGuidString == null) {
            return TypedResults.BadRequest();
        }

        var success = Guid.TryParse(userGuidString, out var userId);

        if (!success) {
            return TypedResults.BadRequest();
        }

        var result = await mediator.Send(new CreateHouseholdCommand(userId, name, description));

        return result.IsFailed
            ? TypedResults.NotFound("User not found")
            : TypedResults.Ok();
    }

    private static async Task<IResult> AddMemberToHousehold(Guid inviteeId,
                                                            Guid requestedById,
                                                            Guid householdId,
                                                            [FromServices] IMediator mediator) {
        await mediator.Send(new AddMemberCommand(householdId, inviteeId, requestedById));

        return TypedResults.Ok();
    }

    private static async Task<IResult> RemoveMemberFromHousehold(
        Guid memberToRemoveId,
        Guid requestedById,
        Guid householdId,
        [FromServices] IMediator mediator) {
        var response = await mediator.Send(new RemoveMemberCommand(memberToRemoveId, requestedById, householdId));

        return response.IsSuccess
            ? TypedResults.Ok(response.Value)
            : TypedResults.NotFound(response.Errors);
    }

    private static async Task<IResult> UpdateHousehold(
        Guid householdId,
        Guid editedById,
        string newName,
        string newDescription,
        [FromServices] IMediator mediator) {
        var response =
            await mediator.Send(new UpdateHouseholdCommand(householdId, editedById, newName, newDescription));

        return TypedResults.Ok(response);
    }

    private static async Task<IResult> GetHouseholdUserList(
        Guid userId,
        Guid householdId,
        [FromServices] IMediator mediator
    ) {
        var response = await mediator.Send(new GetHouseholdUserListQuery(householdId, userId));

        if (response.IsFailed) {
            return TypedResults.BadRequest(response.Errors);
        }

        return TypedResults.Ok(response.Value);
    }

    private static async Task<IResult> GetUserHouseholds(
        Guid userId,
        int pageSize,
        int page,
        [FromServices] IMediator mediator) {
        var response = await mediator.Send(new GetUserHouseholdsQuery(userId, pageSize, page));

        if (response.IsFailed) {
            return TypedResults.BadRequest(response.Errors);
        }

        return TypedResults.Ok(response.Value);
    }

    private static async Task<IResult> GetHousehold(
        Guid householdId,
        Guid userId,
        [FromServices] IMediator mediator) {
        var response = await mediator.Send(new GetHouseholdQuery(userId, householdId));

        if (response.IsFailed) {
            return TypedResults.BadRequest(response.Errors);
        }

        return TypedResults.Ok(response.Value);
    }
}