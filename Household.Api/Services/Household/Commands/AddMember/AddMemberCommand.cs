using MediatR;

namespace Household.Api.Services.Household.Commands.AddMember;

public record AddMemberCommand(Guid HouseholdId, Guid UserId) : IRequest;