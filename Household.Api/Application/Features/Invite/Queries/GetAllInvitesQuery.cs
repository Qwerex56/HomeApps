using HouseholdService.Application.Response;
using MediatR;

namespace HouseholdService.Application.Features.Invite.Queries.GetAllInvites;

public record GetAllInvitesQuery(
    Guid UserId,
    Guid HouseholdId
) : IRequest<IEnumerable<InviteDto>>;