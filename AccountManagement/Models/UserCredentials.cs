namespace AccountManagement.Models;

public class UserCredentials : IEntity {
    public byte[] PasswordHash { get; init; }
    public byte[] PasswordSalt { get; init; }
    public string Algorithm { get; init; } = "PBKDF2";

    public Guid UserId { get; init; }
    public User User { get; init; } = null!;
}