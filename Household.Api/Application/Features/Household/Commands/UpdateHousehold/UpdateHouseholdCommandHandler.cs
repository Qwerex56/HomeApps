using FluentResults;
using HouseholdService.Infrastructure.Extensions;
using HouseholdService.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Authorization;

namespace HouseholdService.Application.Features.Household.Commands.UpdateHousehold;

public class UpdateHouseholdCommandHandler : IRequestHandler<UpdateHouseholdCommand, Result> {
    private readonly HouseholdRepository _householdRepository;
    private readonly UserHouseholdRepository _userHouseholdRepository;

    private readonly UnitOfWork _unitOfWork;

    private readonly ILogger<UpdateHouseholdCommandHandler> _logger;


    public UpdateHouseholdCommandHandler(HouseholdRepository householdRepository,
                                         UserHouseholdRepository userHouseholdRepository,
                                         UnitOfWork unitOfWork,
                                         ILogger<UpdateHouseholdCommandHandler> logger) {
        _householdRepository = householdRepository;
        _userHouseholdRepository = userHouseholdRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateHouseholdCommand request, CancellationToken cancellationToken) {
        var household = await _householdRepository.GetByIdAsync(request.Id, cancellationToken);

        if (household is null) {
            return Result.Fail("HouseholdOrUser.IsNull");
        }

        var editor = household.UserHouseholds.SingleOrDefault(u => u.Id == request.EditorId);

        if (editor is null) {
            return Result.Fail("Editor.IsNull");
        }

        if (!editor.UserFamilyRole.IsAtLeastRole(UserFamilyRoleEnum.FamilyAdmin)) {
            return Result.Fail("Editor.InvalidPermission");
        }

        household.Update(request.EditorId, request.NewName, request.NewDescription);

        try {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException e) {
            _logger.LogError(e.Message);
            return Result.Fail("DbUpdateException");
        }
        
        return Result.Ok();
    }
}