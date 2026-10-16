using TodoListService.Domain.Models;

namespace TodoListService.Domain.Repositories;

public interface ITodoItemRepository {
    public Task<TodoItem> CreteTodoItemAsync(TodoItem todoItem, CancellationToken ct = default);
    
    public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    
    public Task<IEnumerable<TodoItem>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default);
    
    public Task<TodoItem?> DeleteAsync(Guid id, CancellationToken ct = default);
    
    public Task<TodoItem?> UpdateAsync(TodoItem todoItem, CancellationToken ct = default);
}