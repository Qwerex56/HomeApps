using HouseholdService.Domain.Models;
using HouseholdService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HouseholdService.Infrastructure.Repositories;

public class UserRepository : IRepository<User> {
    private readonly HouseholdApiDbContext _context;

    public UserRepository(HouseholdApiDbContext context) {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        var user = await _context.Users
            .Include(u => u.UserHouseholds)
            .Include(u => u.Households)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

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

    public async Task<bool> UserExist(Guid userId, CancellationToken cancellationToken = default) {
        return await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
    }
}