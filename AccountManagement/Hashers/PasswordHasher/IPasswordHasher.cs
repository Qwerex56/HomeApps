namespace AccountManagement.Hashers.PasswordHasher;

public interface IPasswordHasher {
    public (byte[] passwordHash, byte[] passwordSalt) HashPassword(string password);
}