using AccountManagement.Models;
using AccountManagement.Repositories.HouseholdRepository;

namespace AccountManagement.Services.HouseholdService.HouseholdQueryService;

public class HouseholdQueryService : IHouseholdQueryService {
    private readonly ILogger<HouseholdQueryService> _logger;

    private readonly IHouseHoldRepository _householdRepository;

    public HouseholdQueryService(ILogger<HouseholdQueryService> logger, IHouseHoldRepository householdRepository) {
        _logger = logger;
        _householdRepository = householdRepository;
    }

    public async Task<IEnumerable<Household>> GetUserHouseholdsAsync(Guid userId) {
        return await _householdRepository.GetUserAllHouseholdsAsync(userId);
    }

    public async Task<Household?> GetHouseholdAsync(Guid householdId) {
        return await _householdRepository.GetByIdAsync(householdId);
    }
}