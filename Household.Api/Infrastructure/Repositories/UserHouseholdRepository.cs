using HouseholdService.Domain.Models;
using HouseholdService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HouseholdService.Infrastructure.Repositories;

public class UserHouseholdRepository : IRepository<UserHousehold> {
    private readonly HouseholdApiDbContext _context;

    public UserHouseholdRepository(HouseholdApiDbContext context) {
        _context = context;
    }

    public async Task<UserHousehold?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        return await _context.UserHouseholds
            .AsNoTracking()
            .FirstOrDefaultAsync(uh => uh.Id == id, cancellationToken);
    }

    public async Task<UserHousehold?> GetByUserIdAndHouseholdIdAsync(Guid userId,
                                                                     Guid householdId,
                                                                     CancellationToken cancellationToken = default) {
        return await _context.UserHouseholds
            .AsNoTracking()
            .Where(uh => uh.UserId == userId && uh.HouseholdId == householdId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserHousehold>> GetByUserIdAsync(Guid userId,
                                                                   int pageSize = 20,
                                                                   int page = 0,
                                                                   CancellationToken cancellationToken = default) {
        return await _context.UserHouseholds
            .AsNoTracking()
            .Where(uh => uh.UserId == userId)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Include(uh => uh.Household)
            .Include(uh => uh.User)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetItemCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) {
        return await _context.UserHouseholds
            .AsNoTracking()
            .Where(uh => uh.UserId == userId)
            .CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserHousehold>> GetByHouseholdIdAsync(Guid householdId,
                                                                        int pageSize = 20,
                                                                        int page = 0,
                                                                        CancellationToken cancellationToken = default) {
        return await _context.UserHouseholds
            .AsNoTracking()
            .Where(uh => uh.HouseholdId == householdId)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Include(uh => uh.Household)
            .Include(uh => uh.User)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserHousehold entity) {
        await _context.UserHouseholds.AddAsync(entity);
    }

    public Task UpdateAsync(UserHousehold entity) {
        _context.UserHouseholds.Update(entity);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserHousehold entity) {
        _context.UserHouseholds.Remove(entity);

        return Task.CompletedTask;
    }

    public async Task<UserHousehold?> GetByHouseholdAndUserIdAsync(Guid householdId, Guid userId) {
        var userHousehold = await _context
            .UserHouseholds
            .AsNoTracking()
            .FirstOrDefaultAsync(uh => uh.UserId == userId && uh.HouseholdId == householdId);

        return userHousehold;
    }
}