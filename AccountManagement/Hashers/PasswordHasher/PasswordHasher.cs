namespace AccountManagement.Hashers.PasswordHasher;

public class PasswordHasher : IPasswordHasher {
    public (byte[] passwordHash, byte[] passwordSalt) HashPassword(string password) {
        throw new NotImplementedException();
    }
}