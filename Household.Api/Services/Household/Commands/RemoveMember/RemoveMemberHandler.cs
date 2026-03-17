using FluentResults;
using Household.Api.Extensions;
using Household.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Authorization;

namespace Household.Api.Services.Household.Commands.RemoveMember;

public class RemoveMemberHandler : IRequestHandler<RemoveMemberCommand, Result> {
    private readonly HouseholdRepository _householdRepository;

    private readonly UnitOfWork _unitOfWork;

    private readonly ILogger<RemoveMemberHandler> _logger;

    public RemoveMemberHandler(HouseholdRepository householdRepository,
                               UnitOfWork unitOfWork,
                               ILogger<RemoveMemberHandler> logger) {
        _householdRepository = householdRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken cancellationToken) {
        var household = await _householdRepository.GetByIdAsync(request.HouseholdId);

        if (household is null) {
            return Result.Fail("Household.IsNull");
        }

        var memberToRemove = household.UserHouseholds
            .SingleOrDefault(user => user.Id == request.MemberToRemoveId);
        var requestedBy = household.UserHouseholds
            .SingleOrDefault(user => user.Id == request.RequestedById);

        if (memberToRemove is null || requestedBy is null) {
            return Result.Fail("Member.IsNull");
        }

        if (!requestedBy.UserFamilyRole.IsAtLeastRole(UserFamilyRoleEnum.FamilyAdmin) ||
            !requestedBy.UserFamilyRole.IsMorePrivilegedRole(memberToRemove.UserFamilyRole)) {
            return Result.Fail("RequestedBy.NoPermissions");
        }

        household.RemoveHouseholdMember(request.MemberToRemoveId);

        try {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException ex) {
            _logger.LogError(ex.Message);

            return Result.Fail("Db.UpdateException");
        }

        return Result.Ok();
    }
}