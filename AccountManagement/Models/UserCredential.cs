namespace AccountManagement.Models;

public class UserCredential : IEntity {
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public Guid UserId { get; init; }
}