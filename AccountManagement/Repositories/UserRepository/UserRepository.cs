using AccountManagement.Data;
using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Repositories.UserRepository;

public class UserRepository : IUserRepository {
    private readonly AccountDbContext _context;

    public UserRepository(AccountDbContext context) {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync() {
        return await _context.Users
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task CreateAsync(User item) {
        await _context.Users.AddAsync(item);
    }

    public async Task UpdateAsync(User item) {
        var oldUser = await GetByIdAsync(item.Id);

        if (oldUser is null) {
            throw new KeyNotFoundException();
        }
        
        _context.Users
            .Update(item);
    }

    public async Task<User?> GetByIdAsync(Guid id) {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User> DeleteAsync(Guid id) {
        var user = await GetByIdAsync(id);

        if (user is null) {
            throw new KeyNotFoundException();
        }
        
        _context.Users
            .Remove(user);

        return user;
    }

    public async Task CreateUserCredentialsAsync(UserCredential userCredential) {
        await _context.UserCredentials.AddAsync(userCredential);
    }

    public Task CreateExternalCredentialsAsync(ExternalCredentials externalCredentials) {
        throw new NotImplementedException();
    }

    public async Task CreateUserCredentialsAsync(UserCredentials userCredentials) {
        await _context.UserCredentials.AddAsync(userCredentials);
    }
}