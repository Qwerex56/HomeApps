using AccountManagement.Dto.HouseholdDto;
using AccountManagement.Enums;
using AccountManagement.Models;
using AccountManagement.Repositories.HouseholdRepository;
using AccountManagement.Repositories.UserRepository;
using AccountManagement.Workers.UnitOfWork;
using Shared.Authorization;
using Shared.Exceptions.Service;
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
            throw new HouseholdNotFoundException($"Household with ID '{updateHouseholdDto.HouseholdId}' not found.");
        }

        var user = household.UserHouseholds.FirstOrDefault(u => u.UserId == updateHouseholdDto.UserId);

        if (user is null || user.UserHouseholdRole != UserFamilyRoleEnum.FamilyOwner) {
            throw new HouseholdForbiddenException("User is not authorized to update this household.");
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
        if (invitor is null) {
            throw new UserNotFoundException($"Invitor with ID '{addUserToHouseholdDto.InviterId}' not found.");
        }
        
        if (invitee is null) {
            throw new UserNotFoundException($"Invitee with ID '{addUserToHouseholdDto.InviteeId}' not found.");
        }
        
        if (household is null) {
            throw new HouseholdNotFoundException($"Household with ID '{addUserToHouseholdDto.HouseholdId}' not found.");
        }

        // Do invitor is in household and have rule for inviting
        var invitorInHousehold = household.UserHouseholds.FirstOrDefault(uh => uh.UserId == invitor.Id);
        if (invitorInHousehold is null || (invitorInHousehold.UserHouseholdRole != UserFamilyRoleEnum.FamilyOwner &&
                                           invitorInHousehold.UserHouseholdRole != UserFamilyRoleEnum.FamilyAdmin)) {
            throw new HouseholdForbiddenException("User is not authorized to add members to this household.");
        }

        // Is invitee already in household
        if (household.UserHouseholds.FirstOrDefault(uh => uh.UserId == invitee.Id) is not null) {
            throw new HouseholdConflictException("User already exists in household.");
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
        if (remover is null) {
            throw new UserNotFoundException($"Remover with ID '{removeUserFromHouseholdDto.RemoverId}' not found.");
        }
        
        if (userToRemove is null) {
            throw new UserNotFoundException($"User to remove with ID '{removeUserFromHouseholdDto.UserToRemoveId}' not found.");
        }
        
        if (household is null) {
            throw new HouseholdNotFoundException($"Household with ID '{removeUserFromHouseholdDto.HouseholdId}' not found.");
        }

        if (household.UserHouseholds.Count <= 1) {
            throw new HouseholdConflictException("You cannot remove the only user from the household.");
        }
        
        var removerMembership = household.UserHouseholds.FirstOrDefault(uh => uh.UserId == remover.Id);
        if (removerMembership is null || (removerMembership.UserHouseholdRole != UserFamilyRoleEnum.FamilyOwner &&
                                          removerMembership.UserHouseholdRole != UserFamilyRoleEnum.FamilyAdmin)) {
            throw new HouseholdForbiddenException("User is not authorized to remove members from this household.");
        }
        
        var membershipToRemove = household.UserHouseholds.FirstOrDefault(uh => uh.UserId == userToRemove.Id);
        if (membershipToRemove is null) {
            throw new HouseholdConflictException("User already removed or does not exist in household.");
        }

        if (membershipToRemove.UserHouseholdRole == UserFamilyRoleEnum.FamilyOwner) {
            throw new HouseholdForbiddenException("Cannot remove the household owner.");
        }

        if (removerMembership.UserHouseholdRole is UserFamilyRoleEnum.FamilyAdmin &&
            membershipToRemove.UserHouseholdRole != UserFamilyRoleEnum.FamilyMember) {
            throw new HouseholdForbiddenException("Admin users can only remove family members.");
        }

        household.UserHouseholds.Remove(membershipToRemove);

        await _unitOfWork.SaveChangesAsync();
    }
}