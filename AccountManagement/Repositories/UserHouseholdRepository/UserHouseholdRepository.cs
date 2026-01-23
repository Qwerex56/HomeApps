using AccountManagement.Data;
using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Repositories.UserHouseholdRepository;

public class UserHouseholdRepository : IUserHouseHoldRepository {
    private readonly AccountDbContext _context;

    public UserHouseholdRepository(AccountDbContext context) {
        _context = context;
    }

    public async Task<IEnumerable<UserHousehold>> GetAllAsync() {
        return await _context.UserHouseholds
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task CreateAsync(UserHousehold item) {
        await _context.UserHouseholds.AddAsync(item);
    }

    public async Task UpdateAsync(UserHousehold item) {
        _context.UserHouseholds.Update(item);
    }

    public async Task<UserHousehold?> GetByIdAsync(Guid key1, Guid key2) {
        var userHousehold = await _context.UserHouseholds
            .FirstOrDefaultAsync(x => x.UserId == key1 && x.HouseholdId == key2);

        return userHousehold;
    }

    public async Task<UserHousehold> DeleteAsync(Guid key1, Guid key2) {
        var userHousehold = await GetByIdAsync(key1, key2);

        if (userHousehold is null) {
            throw new KeyNotFoundException();
        }

        _context.UserHouseholds.Remove(userHousehold);
        
        return userHousehold;
    }

    public async Task<UserHousehold> DeleteAsync(UserHousehold item) {
        _context.UserHouseholds.Remove(item);
        return item;
    }
}