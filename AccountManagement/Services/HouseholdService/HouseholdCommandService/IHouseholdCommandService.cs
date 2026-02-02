using AccountManagement.Models;

namespace AccountManagement.Services.HouseholdService.HouseholdCommandService;

public interface IHouseholdCommandService {
    public Task CreateHouseholdAsync();
    public Task UpdateHouseholdAsync(Household household);
    public Task DeleteHouseholdAsync(Household household);
}