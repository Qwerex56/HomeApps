using AccountManagement.Models;

namespace AccountManagement.Services.HouseholdService.HouseholdQueryService;

public interface IHouseholdQueryService {
    public Task<IEnumerable<Household>> GetUserHouseholdsAsync(Guid userId);
    public Task<Household?> GetHouseholdAsync(Guid householdId);
}