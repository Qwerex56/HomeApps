using System.Security.Cryptography;
using System.Text;

namespace Devices.Domain.Devices;

/// <summary>
/// One-time pairing codes (jarvis-satellites.md 3a): 8 characters without look-alikes (no 0/O, 1/I/L), easy to read
/// aloud or type on a device. Only the SHA-256 is stored; a code lives 10 minutes and works once.
/// </summary>
public static class PairingCode
{
    public const int Length = 8;
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string New() => RandomNumberGenerator.GetString(Alphabet, Length);

    /// <summary>The stored form; spaces, dashes and case do not matter.</summary>
    public static string Hash(string code)
    {
        var normalized = new string([.. (code ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant)]);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
