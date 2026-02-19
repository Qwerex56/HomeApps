using AccountManagement.Repositories.HouseholdRepository;
using AccountManagement.Services.HouseholdService.HouseholdQueryService;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountManagement.Tests.Unit.Services.Household;

public class HouseholdTestBase {
    protected readonly Mock<ILogger<HouseholdQueryService>> Logger;
    protected readonly Mock<IHouseHoldRepository> HouseholdRepository;
    
    public HouseholdTestBase() {
        HouseholdRepository = new Mock<IHouseHoldRepository>();
        Logger = new Mock<ILogger<HouseholdQueryService>>();
    }
}