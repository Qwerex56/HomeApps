using AccountManagement.Dto.HouseholdDto;
using AccountManagement.Models;

namespace AccountManagement.Services.HouseholdService.HouseholdQueryService;

public interface IHouseholdQueryService {
    public Task<IEnumerable<HouseholdDto>> GetUserHouseholdsAsync(Guid userId);
    public Task<HouseholdDto?> GetHouseholdAsync(Guid householdId);
}