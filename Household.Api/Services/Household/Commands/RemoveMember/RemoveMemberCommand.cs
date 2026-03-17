using FluentResults;
using MediatR;

namespace Household.Api.Services.Household.Commands.RemoveMember;

public record RemoveMemberCommand(Guid MemberToRemoveId, Guid HouseholdId, Guid RequestedById) : IRequest<Result>;