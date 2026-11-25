namespace Accounts.Application.Common;

/// <summary>2.15: tells a picture's type from its first bytes, never from the file name or the client's header.</summary>
public static class AvatarImage
{
    public const int MaxBytes = 2 * 1024 * 1024;

    /// <summary>The content type for JPEG, PNG and WebP; null for anything else.</summary>
    public static string? Sniff(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
        {
            return "image/jpeg";
        }

        if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return "image/png";
        }

        return bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8)
            ? "image/webp"
            : null;
    }
}
