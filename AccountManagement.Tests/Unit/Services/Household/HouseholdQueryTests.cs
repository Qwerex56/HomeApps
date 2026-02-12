using AccountManagement.Models;
using AccountManagement.Services.HouseholdService.HouseholdQueryService;
using Moq;

namespace AccountManagement.Tests.Unit.Services.Household;

public class HouseholdQueryTests : HouseholdTestBase {
    private HouseholdQueryService _householdQueryService;

    public HouseholdQueryTests() : base() {
        _householdQueryService = new HouseholdQueryService(
            Logger.Object,
            HouseholdRepository.Object
        );
    }

    [Fact]
    public async Task GetUserHouseholdsAsync_ShouldReturnUserHouseholds_WhenUserIdIsValid() {
        var userId =  Guid.NewGuid();
        
        HouseholdRepository.Setup(s => s.GetUserAllHouseholdsAsync(userId))
            .ReturnsAsync([
                new Models.Household {
                    FamilyName = "SomeName"
                }
            ]);
        
        var result = await _householdQueryService.GetUserHouseholdsAsync(userId);
        
        Assert.IsAssignableFrom<IEnumerable<Models.Household>>(result);
        
        HouseholdRepository.Verify(s => s.GetUserAllHouseholdsAsync(userId), Times.Once);
    }
    
    [Fact]
    public async Task GetHouseholdAsync_ShouldReturnUserHousehold_WhenIdIsValid() {
        var householdId =  Guid.NewGuid();
        
        HouseholdRepository.Setup(s => s.GetByIdAsync(householdId))
            .ReturnsAsync(
                new Models.Household {
                    FamilyName = "SomeName"
                }
            );
        
        var result = await _householdQueryService.GetHouseholdAsync(householdId);
        
        Assert.IsAssignableFrom<IEnumerable<Models.Household>>(result);
        
        HouseholdRepository.Verify(s => s.GetByIdAsync(householdId), Times.Once);
    }
}