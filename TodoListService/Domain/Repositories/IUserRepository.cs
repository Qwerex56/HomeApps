using TodoListService.Domain.Models;

namespace TodoListService.Domain.Repositories;

public interface IUserRepository {
    Task<User?> GetUserById(Guid id, CancellationToken ct = default);
    
    Task<IEnumerable<User>> GetAllUsers(CancellationToken ct = default);

    Task<User> CreateUser(User user, CancellationToken ct = default);

    Task<User?> UpdateUser(User user, CancellationToken ct = default);
    
    Task DeleteUser(Guid userId, CancellationToken ct = default);
}