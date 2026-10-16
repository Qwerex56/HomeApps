namespace TodoListService.Domain.Models;

public class User {
    public Guid Id { get; set; }
    public required string Name { get; set; }
    
    public IEnumerable<TodoList> TodoLists { get; set; } = [];
}