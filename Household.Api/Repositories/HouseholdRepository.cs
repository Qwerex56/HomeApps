using Household.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Household.Api.Repositories;

public class HouseholdRepository : IRepository<Models.Household> {
    private readonly HouseholdApiDbContext _context;

    public HouseholdRepository(HouseholdApiDbContext context) {
        _context = context;
    }

    public async Task<Models.Household?> GetByIdAsync(Guid id) {
        var household = await _context.Households
            .Include(h => h.Users)
            .Include(h => h.UserHouseholds)
            .FirstOrDefaultAsync(h => h.Id == id);

        return household;
    }

    public async Task AddAsync(Models.Household entity) {
        await _context.Households.AddAsync(entity);
    }

    public Task UpdateAsync(Models.Household entity) {
        _context.Households.Update(entity);
        
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Models.Household entity) {
        _context.Households.Remove(entity);
        
        return Task.CompletedTask;
    }
}