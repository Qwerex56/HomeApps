namespace AccountManagement.Models;

public class Household {
    public int Id { get; set; }
    public required string FamilyName { get; set; }
    public DateTime Created { get; set; }

    public ICollection<User> Members { get; set; } = [];
}