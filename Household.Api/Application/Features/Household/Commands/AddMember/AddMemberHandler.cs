using FluentResults;
using HouseholdService.Application.Response;
using HouseholdService.Domain.Models;
using HouseholdService.Infrastructure.Extensions;
using HouseholdService.Infrastructure.Repositories;
using MediatR;
using Shared.Authorization;

namespace HouseholdService.Application.Features.Household.Commands.AddMember;

public class AddMemberHandler : IRequestHandler<AddMemberCommand, Result<AddMemberCommandResponse>> {
    private readonly ILogger<AddMemberHandler> _logger;

    private readonly UserHouseholdRepository _userHouseholdRepository;
    private readonly UserRepository _userRepository;

    private readonly UnitOfWork _unitOfWork;

    public AddMemberHandler(ILogger<AddMemberHandler> logger,
                            UserHouseholdRepository userHouseholdRepository,
                            UserRepository userRepository,
                            UnitOfWork unitOfWork) {
        _logger = logger;
        _userHouseholdRepository = userHouseholdRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AddMemberCommandResponse>> Handle(AddMemberCommand request,
                                                               CancellationToken cancellationToken) {
        var inviterUserHousehold = await _userHouseholdRepository
            .GetByUserIdAndHouseholdIdAsync(request.InviterId, request.HouseholdId, cancellationToken);

        if (inviterUserHousehold is null) {
            return Result.Fail("NotFound.InviterOrHousehold");
        }

        if (!await _userRepository.UserExist(request.InviterId, cancellationToken)) {
            return Result.Fail("Invitee.NotFound");
        }

        if (!inviterUserHousehold.UserFamilyRole.IsAtLeastRole(UserFamilyRoleEnum.FamilyAdmin)) {
            return Result.Fail("Inviter.NotPrivileged");
        }

        var userHousehold = UserHousehold.Create(
            householdId: request.HouseholdId,
            userId: request.InviteeId,
            nickname: string.Empty,
            userFamilyRole: UserFamilyRoleEnum.FamilyMember
        );

        await _unitOfWork.UserHouseholdRepository.AddAsync(userHousehold);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok(new AddMemberCommandResponse());
    }
}