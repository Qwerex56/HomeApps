namespace Auth.Domain.Common;

/// <summary>Hashes and checks passwords. Implemented in Infrastructure with PBKDF2 (<c>PasswordHasher&lt;T&gt;</c>).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// False for a wrong password. Must also return false, not throw, for an empty or malformed hash: sign-in calls it
    /// for identities without a password too, so the response takes the same time (R4).
    /// </summary>
    bool Verify(string hash, string password);
}
