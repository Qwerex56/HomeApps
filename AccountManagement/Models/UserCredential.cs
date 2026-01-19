namespace AccountManagement.Models;

public class UserCredential : IEntity {
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public Guid UserId { get; init; }
    public User User { get; init; } = null!;
}