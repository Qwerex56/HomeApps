using AccountManagement.Models;

namespace AccountManagement.Repositories.HouseholdRepository;

public interface IHouseHoldRepository : ISimpleRepository<Household, Guid> {
    public Task<IEnumerable<Household>> GetUserAllHouseholdsAsync(Guid userId);
}