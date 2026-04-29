using HouseholdService.Application.Features.Invite.Queries.GetAllInvites;
using HouseholdService.Application.Response;
using HouseholdService.Infrastructure.Repositories;
using MediatR;

namespace HouseholdService.Application.Features.Invite.Queries;

public class GetAllInvitesQueryHandler : IRequestHandler<GetAllInvitesQuery, IEnumerable<InviteDto>> {
    private readonly InviteRepository _inviteRepository;
    private readonly UserHouseholdRepository _userHouseholdRepository;

    public GetAllInvitesQueryHandler(InviteRepository inviteRepository, UserHouseholdRepository userHouseholdRepository) {
        _inviteRepository = inviteRepository;
        _userHouseholdRepository = userHouseholdRepository;
    }

    public async Task<IEnumerable<InviteDto>> Handle(GetAllInvitesQuery request, CancellationToken cancellationToken) {
        var userHousehold =
            await _userHouseholdRepository
                .GetByHouseholdAndUserIdAsync(request.HouseholdId, request.UserId);

        // if not null then we know that given user exists in given household
        if (userHousehold is null) {
            throw new Exception("Invite not found");
        }
        
        var invites = await _inviteRepository.GetAllInHousehold(request.HouseholdId);

        return InviteMapper.ToInviteDtos(invites);
    }
}