using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Domain.Common;

/// <summary>
/// Random secrets handed to a client once (refresh tokens, links in emails, invitation links).
/// Only the SHA-256 hash is ever stored; the raw value leaves the domain exactly once.
/// </summary>
public static class OpaqueToken
{
    private const int SizeInBytes = 32;

    public static (string Raw, string Hash) New()
    {
        var raw = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SizeInBytes));
        return (raw, HashOf(raw));
    }

    public static string HashOf(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
