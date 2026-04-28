using HouseholdService.Domain.Models;
using HouseholdService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HouseholdService.Infrastructure.Repositories;

public class HouseholdRepository : IRepository<HouseholdService.Domain.Models.Household> {
    private readonly HouseholdApiDbContext _context;

    public HouseholdRepository(HouseholdApiDbContext context) {
        _context = context;
    }

    public async Task<Domain.Models.Household?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        var household = await _context.Households
            .AsNoTracking()
            .Include(h => h.Users)
            .Include(h => h.UserHouseholds)
            .FirstOrDefaultAsync(h => h.Id == id);

        return household;
    }

    public async Task<IEnumerable<HouseholdService.Domain.Models.Household>> GetPageAsync(
        Guid userId,
        int page = 0,
        int pageSize = 20,
        CancellationToken cancellationToken = default) {
        return await _context.Households
            .Where(h => h.Users.Any(u => u.Id == userId))
            .Skip(page * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Include(h => h.Users)
            .Include(h => h.UserHouseholds)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<UserHousehold>> GetUserListAsync(Guid householdId,
                                                                   int page = 0,
                                                                   int pageSize = 20,
                                                                   CancellationToken cancellationToken = default) {
        return await _context.UserHouseholds
            .Where(h => h.HouseholdId == householdId)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(HouseholdService.Domain.Models.Household entity) {
        await _context.Households.AddAsync(entity);
    }

    public Task UpdateAsync(HouseholdService.Domain.Models.Household entity) {
        _context.Households.Update(entity);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(HouseholdService.Domain.Models.Household entity) {
        _context.Households.Remove(entity);

        return Task.CompletedTask;
    }
}