using AccountService.Domain.Models;

namespace AccountService.Infrastructure.Repositories.UserRepository;

public interface IUserRepository : ISimpleRepository<User, Guid> {
}