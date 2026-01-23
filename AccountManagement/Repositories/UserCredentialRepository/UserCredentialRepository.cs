using AccountManagement.Data;
using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Repositories.UserCredentialRepository;

public class UserCredentialRepository : IUserCredentialRepository {
    private readonly AccountDbContext _context;

    public UserCredentialRepository(AccountDbContext context) {
        _context = context;
    }

    public async Task<IEnumerable<UserCredential>> GetAllAsync() {
        return await _context.UserCredentials
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task CreateAsync(UserCredential item) {
        await _context.UserCredentials.AddAsync(item);
    }

    public async Task UpdateAsync(UserCredential item) {
        _context.UserCredentials.Update(item);
    }

    public async Task<UserCredential> DeleteAsync(UserCredential item) {
        _context.UserCredentials.Remove(item);

        return item;
    }

    public async Task<UserCredential?> GetByEmailAsync(string email) {
        return await _context.UserCredentials
            .FirstOrDefaultAsync(c => c.Email == email);
    }
}