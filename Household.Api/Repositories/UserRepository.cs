using Household.Api.Data;
using Household.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Household.Api.Repositories;

public class UserRepository : IRepository<User> {
    private readonly HouseholdApiDbContext _context;

    public UserRepository(HouseholdApiDbContext context) {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id) {
        var user = await _context.Users
            .Include(u => u.UserHouseholds)
            .Include(u => u.Households)
            .FirstOrDefaultAsync(u => u.Id == id);

        return user;
    }

    public async Task AddAsync(User entity) {
        await _context.Users.AddAsync(entity);
    }

    public Task UpdateAsync(User entity) { 
        _context.Users.Update(entity);
        
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User entity) {
        _context.Users.Remove(entity);
        
        return Task.CompletedTask;
    }
}