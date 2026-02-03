using AccountManagement.Dto.HouseholdDto;
using AccountManagement.Enums;
using AccountManagement.Models;
using AccountManagement.Repositories.HouseholdRepository;
using AccountManagement.Workers.UnitOfWork;
using Shared.Authorization;
using Shared.Validators;

namespace AccountManagement.Services.HouseholdService.HouseholdCommandService;

public class HouseholdCommandService : IHouseholdCommandService {
    private readonly ILogger<HouseholdCommandService> _logger;

    private readonly IHouseHoldRepository _householdRepository;

    private readonly IUnitOfWork _unitOfWork;

    public HouseholdCommandService(ILogger<HouseholdCommandService> logger,
        IHouseHoldRepository householdRepository,
        IUnitOfWork unitOfWork) {
        _logger = logger;
        _householdRepository = householdRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task CreateHouseholdAsync(CreateHouseholdDto createHouseholdDto) {
        var householdName = createHouseholdDto.FamilyName.Trim();

        NameValidator.Validate(householdName);

        var household = new Household {
            FamilyName = householdName,
            Created = DateTime.UtcNow,

            UserHouseholds = {
                new UserHousehold {
                    UserId = createHouseholdDto.CreatorId,
                    UserHouseholdRole = UserFamilyRoleEnum.FamilyOwner,

                    JoinDate = DateTime.UtcNow,
                    FamilyFunction = FamilyFunctionEnum.Other
                }
            }
        };

        await _householdRepository.CreateAsync(household);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateHouseholdAsync(UpdateHouseholdDto updateHouseholdDto) {
        var household = await _householdRepository.GetByIdAsync(updateHouseholdDto.HouseholdId);

        if (household is null) {
            throw new KeyNotFoundException();
        }

        var user = household.UserHouseholds.FirstOrDefault(u => u.UserId == updateHouseholdDto.UserId);

        if (user is null || user.UserHouseholdRole != UserFamilyRoleEnum.FamilyOwner) {
            throw new UnauthorizedAccessException(); // Unauthorized
        }

        var familyName = updateHouseholdDto.FamilyName.Trim();
        NameValidator.Validate(familyName);

        household.FamilyName = familyName;
        
        await _householdRepository.UpdateAsync(household);
        await _unitOfWork.SaveChangesAsync();
    }

    public Task DeleteHouseholdAsync(Household household) {
        throw new NotImplementedException();
    }

    public Task AddUserToHouseholdAsync(Guid userId, Guid householdId) {
        throw new NotImplementedException();
    }

    public Task RemoveUserFromHouseholdAsync(Guid userId, Guid householdId) {
        throw new NotImplementedException();
    }
}