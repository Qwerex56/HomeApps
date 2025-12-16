namespace AccountManagement.Models;

public class User {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Should autogen when empty on creation
    public required string Email { get; set; }

    public int? HouseholdId { get; set; } = null; // foreign key
    public Household? Household { get; set; } = null;
    
    public ICollection<Account> Accounts { get; set; } = [];
}