using FluentResults;
using HouseholdService.Application.Response;
using MediatR;

namespace HouseholdService.Application.Features.Household.Commands.RemoveMember;

public record RemoveMemberCommand(
    Guid MemberToRemoveId,
    Guid HouseholdId,
    Guid RequestedById
) : IRequest<Result<RemoveMemberResponse>>;