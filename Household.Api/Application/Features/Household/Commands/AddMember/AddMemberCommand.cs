using FluentResults;
using HouseholdService.Application.Response;
using MediatR;

namespace HouseholdService.Application.Features.Household.Commands.AddMember;

public record AddMemberCommand(
    Guid HouseholdId,
    Guid InviteeId,
    Guid InviterId
) : IRequest<Result<AddMemberCommandResponse>>;