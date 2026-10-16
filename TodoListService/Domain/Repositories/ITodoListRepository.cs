using TodoListService.Domain.Models;

namespace TodoListService.Domain.Repositories;

public interface ITodoListRepository {
    public Task<User> CreateUserAsync(User user, CancellationToken ct = default);
    
    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    
    public Task<User> UpdateUserAsync(User user, CancellationToken ct = default);
    
    public Task DeleteUserAsync(Guid userId, CancellationToken ct = default);
}