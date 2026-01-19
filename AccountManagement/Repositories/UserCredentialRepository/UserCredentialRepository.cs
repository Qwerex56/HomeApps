using AccountManagement.Data;
using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Repositories.UserCredentialRepository;

public class UserCredentialRepository : IUserCredentialRepository {
    private readonly AccountDbContext _dbContext;

    public UserCredentialRepository(AccountDbContext dbContext) {
        _dbContext = dbContext;
    }

    public Task<IEnumerable<UserCredential>> GetAllAsync() {
        throw new NotImplementedException();
    }

    public Task CreateAsync(UserCredential item) {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(UserCredential item) {
        throw new NotImplementedException();
    }

    public async Task<UserCredential?> GetByEmailAsync(string email) {
        return await _dbContext.UserCredentials
            .FirstOrDefaultAsync(c => c.Email == email);
    }
}