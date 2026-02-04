using AccountManagement.Dto.HouseholdDto;
using AccountManagement.Models;

namespace AccountManagement.Services.HouseholdService.HouseholdCommandService;

public interface IHouseholdCommandService {
    public Task CreateHouseholdAsync(CreateHouseholdDto createHouseholdDto);
    public Task UpdateHouseholdAsync(UpdateHouseholdDto updateHouseholdDto);
    public Task DeleteHouseholdAsync(Household household);
    
    public Task AddUserToHouseholdAsync(AddUserToHouseholdDto addUserToHouseholdDto);
    public Task RemoveUserFromHouseholdAsync(RemoveUserFromHouseholdDto removeUserFromHouseholdDto);
}