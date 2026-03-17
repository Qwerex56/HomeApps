using Household.Api.Data;
using Household.Api.Models;

namespace Household.Api.Repositories;

public class UserHouseholdRepository : IRepository<UserHousehold> {
    private readonly HouseholdApiDbContext _context;

    public UserHouseholdRepository(HouseholdApiDbContext context) {
        _context = context;
    }

    public Task<UserHousehold?> GetByIdAsync(Guid id) {
        throw new NotImplementedException();
    }

    public Task AddAsync(UserHousehold entity) {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(UserHousehold entity) {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(UserHousehold entity) {
        throw new NotImplementedException();
    }
}