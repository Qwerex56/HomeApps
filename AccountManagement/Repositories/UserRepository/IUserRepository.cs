using AccountManagement.Models;

namespace AccountManagement.Repositories.UserRepository;

public interface IUserRepository : ISimpleRepository<User, Guid> {
}