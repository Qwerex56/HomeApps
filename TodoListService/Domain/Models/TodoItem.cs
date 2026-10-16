namespace TodoListService.Domain.Models;

public class TodoItem {
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsComplete { get; private set; } = false;
    
    public Guid TodoListId { get; set; }
    public TodoList TodoList { get; set; } = null!;

    public void MarkAsComplete() {
        IsComplete = true;
    }
    
    public void MarkAsIncomplete() {
        IsComplete = false;
    }
    
    public void Rename(string newTitle) {
        if (string.IsNullOrEmpty(newTitle)) {
            throw new ArgumentNullException(nameof(newTitle));
        }
        
        Title = newTitle;
    }
    
    public void ChangeDescription(string newDescription) {
        Description = newDescription;
    }
}