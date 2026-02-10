using AccountManagement.Dto.HouseholdDto;
using AccountManagement.Mappers;
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

    public async Task<IEnumerable<HouseholdDto>> GetUserHouseholdsAsync(Guid userId) {
        var data = await _householdRepository.GetUserAllHouseholdsAsync(userId);
        return HouseholdMapper.ToHouseholdDtos(data);
    }

    public async Task<HouseholdDto?> GetHouseholdAsync(Guid householdId) {
        var data = await _householdRepository.GetByIdAsync(householdId);

        if (data is null) {
            return null;
        }
        
        return HouseholdMapper.ToHouseholdDto(data);
    }
}