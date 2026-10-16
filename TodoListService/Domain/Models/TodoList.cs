namespace TodoListService.Domain.Models;

public class TodoList {
    public Guid Id { get; set; }
    public required string ListName { get; set; }
    public string Description { get; set; } = string.Empty;
    
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
}