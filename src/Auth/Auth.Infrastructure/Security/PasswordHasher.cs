using Auth.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Security;

/// <summary>PBKDF2 from ASP.NET Core Identity (format v3, the framework's current iteration count and salt).</summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<HashedPassword> Inner = new();

    /// <summary>Checked instead of a missing hash, so an account without a password answers as slowly as any other (R4).</summary>
    private static readonly string DummyHash = Inner.HashPassword(HashedPassword.Instance, Guid.NewGuid().ToString());

    public string Hash(string password) => Inner.HashPassword(HashedPassword.Instance, password);

    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrEmpty(hash))
        {
            Inner.VerifyHashedPassword(HashedPassword.Instance, DummyHash, password ?? string.Empty);
            return false;
        }

        try
        {
            return Inner.VerifyHashedPassword(HashedPassword.Instance, hash, password ?? string.Empty)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>The hasher wants a user type; the hash does not depend on it.</summary>
    private sealed class HashedPassword
    {
        public static readonly HashedPassword Instance = new();
    }
}
