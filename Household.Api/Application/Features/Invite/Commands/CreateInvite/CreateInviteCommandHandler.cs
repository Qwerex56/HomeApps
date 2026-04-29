using FluentResults;
using HouseholdService.Infrastructure.Extensions;
using HouseholdService.Infrastructure.Repositories;
using MediatR;
using Shared.Authorization;

namespace HouseholdService.Application.Features.Invite.Commands.CreateInvite;

public class CreateInviteCommandHandler : IRequestHandler<CreateInviteCommand, Result> {
    private readonly InviteRepository _inviteRepository;
    private readonly HouseholdRepository _householdRepository;

    private readonly UnitOfWork _unitOfWork;

    private readonly ILogger<CreateInviteCommandHandler> _logger;

    public CreateInviteCommandHandler(InviteRepository inviteRepository,
                                      HouseholdRepository householdRepository,
                                      UnitOfWork unitOfWork,
                                      ILogger<CreateInviteCommandHandler> logger) {
        _inviteRepository = inviteRepository;
        _householdRepository = householdRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CreateInviteCommand request, CancellationToken cancellationToken) {
        var household = await _householdRepository.GetByIdAsync(request.HouseholdId);

        if (household is null) {
            return Result.Fail("Household.IsNull");
        }

        var inviter = household.UserHouseholds
            .SingleOrDefault(u => u.UserId == request.InviterId);

        if (inviter is null) {
            return Result.Fail("Inviter.IsNull");
        }

        if (!inviter.UserFamilyRole.IsAtLeastRole(UserFamilyRoleEnum.FamilyAdmin)) {
            return Result.Fail("Inviter.PermissionDenied");
        }

        var invite = HouseholdService.Domain.Models.Invite.Create(request.HouseholdId, request.InviterId);
        
        await _inviteRepository.AddAsync(invite);
        await _unitOfWork.SaveChangesAsync();
        
        return Result.Ok();
    }
}