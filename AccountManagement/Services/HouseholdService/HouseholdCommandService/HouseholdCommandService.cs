using AccountManagement.Dto.HouseholdDto;
using AccountManagement.Enums;
using AccountManagement.Models;
using AccountManagement.Repositories.HouseholdRepository;
using AccountManagement.Repositories.UserRepository;
using AccountManagement.Workers.UnitOfWork;
using Shared.Authorization;
using Shared.Validators;

namespace AccountManagement.Services.HouseholdService.HouseholdCommandService;

public class HouseholdCommandService : IHouseholdCommandService {
    private readonly ILogger<HouseholdCommandService> _logger;

    private readonly IHouseHoldRepository _householdRepository;
    private readonly IUserRepository _userRepository;

    private readonly IUnitOfWork _unitOfWork;

    public HouseholdCommandService(ILogger<HouseholdCommandService> logger,
        IHouseHoldRepository householdRepository,
        IUnitOfWork unitOfWork, IUserRepository userRepository) {
        _logger = logger;
        _householdRepository = householdRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
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

    public async Task AddUserToHouseholdAsync(AddUserToHouseholdDto addUserToHouseholdDto) {
        var invitor = await _userRepository.GetByIdAsync(addUserToHouseholdDto.InviterId);
        var invitee = await _userRepository.GetByIdAsync(addUserToHouseholdDto.InviteeId);
        var household = await _householdRepository.GetByIdAsync(addUserToHouseholdDto.HouseholdId);

        // Do they exist
        if (invitor is null || invitee is null || household is null) {
            throw new KeyNotFoundException();
        }

        // Do invitor is in household and have rule for inviting
        var invitorInHousehold = household.UserHouseholds.FirstOrDefault(uh => uh.UserId == invitor.Id);
        if (invitorInHousehold is null || (invitorInHousehold.UserHouseholdRole != UserFamilyRoleEnum.FamilyOwner &&
                                           invitorInHousehold.UserHouseholdRole != UserFamilyRoleEnum.FamilyAdmin)) {
            throw new UnauthorizedAccessException();
        }

        // Is invitee already in household
        if (household.UserHouseholds.FirstOrDefault(uh => uh.UserId == invitee.Id) is not null) {
            throw new InvalidOperationException("User already exists in household");
        }

        // Logic for adding user to household
        household.UserHouseholds.Add(new UserHousehold {
            UserId = invitee.Id,
            FamilyFunction = addUserToHouseholdDto.FamilyFunction,
            UserHouseholdRole = UserFamilyRoleEnum.FamilyMember,

            JoinDate = DateTime.UtcNow,

            Nickname = invitee.Name
        });

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveUserFromHouseholdAsync(RemoveUserFromHouseholdDto removeUserFromHouseholdDto) {
        var remover = await _userRepository.GetByIdAsync(removeUserFromHouseholdDto.RemoverId);
        var userToRemove = await _userRepository.GetByIdAsync(removeUserFromHouseholdDto.UserToRemoveId);
        var household = await _householdRepository.GetByIdAsync(removeUserFromHouseholdDto.HouseholdId);

        // Do they exist
        if (remover is null || userToRemove is null || household is null) {
            throw new KeyNotFoundException();
        }

        if (household.UserHouseholds.Count <= 1) {
            throw new InvalidOperationException("You cannot remove the only user from the household");
        }
        
        var removerMembership = household.UserHouseholds.FirstOrDefault(uh => uh.UserId == remover.Id);
        if (removerMembership is null || (removerMembership.UserHouseholdRole != UserFamilyRoleEnum.FamilyOwner &&
                                          removerMembership.UserHouseholdRole != UserFamilyRoleEnum.FamilyAdmin)) {
            throw new UnauthorizedAccessException();
        }
        
        var membershipToRemove = household.UserHouseholds.FirstOrDefault(uh => uh.UserId == userToRemove.Id);
        if (membershipToRemove is null) {
            throw new InvalidOperationException("User already removed or does not exist in household");
        }

        if (membershipToRemove.UserHouseholdRole == UserFamilyRoleEnum.FamilyOwner) {
            throw new UnauthorizedAccessException();
        }

        if (removerMembership.UserHouseholdRole is UserFamilyRoleEnum.FamilyAdmin &&
            membershipToRemove.UserHouseholdRole != UserFamilyRoleEnum.FamilyMember) {
            throw new UnauthorizedAccessException();
        }

        household.UserHouseholds.Remove(membershipToRemove);

        await _unitOfWork.SaveChangesAsync();
    }
}