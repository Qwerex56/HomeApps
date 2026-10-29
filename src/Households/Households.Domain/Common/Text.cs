namespace Households.Domain.Common;

/// <summary>Small helpers for free text typed by people or copied from Accounts.</summary>
internal static class Text
{
    /// <summary>Trimmed text, or null when blank.</summary>
    public static string? OrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool HasControlCharacters(string value) => value.Any(char.IsControl);

    /// <summary>For trusted input that must not fail: control characters dropped, trimmed, cut to <paramref name="maxLength"/>.</summary>
    public static string? Sanitize(string? value, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        var clean = new string(value.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (clean.Length > maxLength)
        {
            clean = clean[..maxLength].TrimEnd();
        }

        return clean.Length == 0 ? null : clean;
    }
}
