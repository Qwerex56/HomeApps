using AccountManagement.Models;

namespace AccountManagement.Repositories.UserRepository;

public interface IUserRepository : ISimpleRepository<User, Guid> {
    public Task<User?> GetByEmailAsync(string email);
}