using FluentResults;
using HouseholdService.Infrastructure.Extensions;
using HouseholdService.Infrastructure.Repositories;
using MediatR;
using Shared.Authorization;

namespace HouseholdService.Application.Features.Invite.Commands.RevokeInvite;

public class RevokeInviteCommandHandler : IRequestHandler<RevokeInviteCommand, Result> {
    private readonly InviteRepository _inviteRepository;
    private readonly UserRepository _userRepository;
    
    private readonly UnitOfWork _unitOfWork;
    
    private readonly ILogger<RevokeInviteCommandHandler> _logger;

    public RevokeInviteCommandHandler(InviteRepository inviteRepository, UserRepository userRepository, UnitOfWork unitOfWork, ILogger<RevokeInviteCommandHandler> logger) {
        _inviteRepository = inviteRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeInviteCommand request, CancellationToken cancellationToken) {
        var invite = await _inviteRepository.GetByIdAsync(request.InviteId);

        if (invite is null) {
            return Result.Fail("Invite.IsNull");
        }

        var householdId = invite.HouseholdId; 
        var user = await _userRepository.GetByIdAsync(request.UserId);
        
        if (user is null) {
            return Result.Fail("User.IsNull");
        }
        
        var userHousehold = user?.UserHouseholds.FirstOrDefault(x => x.HouseholdId == householdId);

        if (userHousehold != null && !userHousehold.UserFamilyRole.IsAtLeastRole(UserFamilyRoleEnum.FamilyAdmin)) {
            return Result.Fail("User.PermissionDenied");
        }
        
        invite.RevokeInvitation();
        await _unitOfWork.SaveChangesAsync();
        
        return Result.Ok();
    }
}