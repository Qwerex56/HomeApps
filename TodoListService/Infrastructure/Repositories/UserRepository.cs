using TodoListService.Domain.Models;
using TodoListService.Domain.Repositories;

namespace TodoListService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public UserRepository(IUnitOfWork unitOfWork) {
        _unitOfWork = unitOfWork;
    }

    public async Task<User?> GetUserById(Guid id, CancellationToken ct = default) {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<User>> GetAllUsers(CancellationToken ct = default) {
        throw new NotImplementedException();
    }

    public async Task<User> CreateUser(User user, CancellationToken ct = default) {
        throw new NotImplementedException();
    }

    public async Task<User?> UpdateUser(User user, CancellationToken ct = default) {
        throw new NotImplementedException();
    }

    public async Task DeleteUser(Guid userId, CancellationToken ct = default) {
        throw new NotImplementedException();
    }
}