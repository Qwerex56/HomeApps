using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Domain.ConnectedApps;

/// <summary>Proof Key for Code Exchange (RFC 7636), S256 only, as OAuth 2.1 requires.</summary>
public static class Pkce
{
    public const string Method = "S256";
    public const int MinLength = 43;
    public const int MaxLength = 128;

    /// <summary>A challenge is the base64url SHA-256 of the verifier: exactly 43 characters.</summary>
    public static bool IsValidChallenge(string? challenge) =>
        challenge is { Length: MinLength } && challenge.All(IsBase64UrlChar);

    public static bool Verify(string? verifier, string challenge)
    {
        if (verifier is null || verifier.Length is < MinLength or > MaxLength || !verifier.All(IsVerifierChar))
        {
            return false;
        }

        var computed = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(computed), Encoding.ASCII.GetBytes(challenge));
    }

    private static bool IsBase64UrlChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '_';

    private static bool IsVerifierChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~';
}
