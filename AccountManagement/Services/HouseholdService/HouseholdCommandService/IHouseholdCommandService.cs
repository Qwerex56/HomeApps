using AccountManagement.Models;

namespace AccountManagement.Services.HouseholdService.HouseholdCommandService;

public interface IHouseholdCommandService {
    public Task CreateHouseholdAsync();
    public Task UpdateHouseholdAsync(Household household);
    public Task DeleteHouseholdAsync(Household household);
    
    public Task AddUserToHouseholdAsync(Guid userId, Guid householdId);
    public Task RemoveUserFromHouseholdAsync(Guid userId, Guid householdId);
}