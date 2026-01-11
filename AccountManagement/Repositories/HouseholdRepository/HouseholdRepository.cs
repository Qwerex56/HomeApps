using AccountManagement.Data;
using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Repositories.HouseholdRepository;

public class HouseholdRepository : IHouseHoldRepository {
    private readonly AccountDbContext _context;

    public HouseholdRepository(AccountDbContext context) {
        _context = context;
    }

    public async Task<IEnumerable<Household>> GetAllAsync() {
        return await _context.Households
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task CreateAsync(Household item) {
        await _context.Households.AddAsync(item);
    }

    public async Task UpdateAsync(Household item) {
        var household = await GetByIdAsync(item.Id);

        if (household is null) {
            throw new KeyNotFoundException();
        }
        
        _context.Households.Update(item);
    }

    public async Task<Household?> GetByIdAsync(Guid id) {
        var household = await _context.Households.FirstOrDefaultAsync(h => h.Id == id);
        
        return household;
    }

    public async Task<Household> DeleteAsync(Guid id) {
        var household = await GetByIdAsync(id);

        if (household is null) {
            throw new KeyNotFoundException();
        }

        _context.Households.Remove(household);
        
        return household;
    }
}