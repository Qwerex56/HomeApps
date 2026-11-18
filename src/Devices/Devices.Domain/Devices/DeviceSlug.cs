using System.Globalization;
using System.Text;

namespace Devices.Domain.Devices;

/// <summary>
/// The key made from a device's name when it is added ("Fotel taty w salonie" becomes "fotel-taty-w-salonie"), with a
/// number after it when the household already has that key. It never changes, even if the device is renamed.
/// </summary>
public static class DeviceSlug
{
    public const int MaxLength = 40;
    private const string Fallback = "device";

    public static string From(string name, IReadOnlyCollection<string> taken)
    {
        ArgumentNullException.ThrowIfNull(taken);
        var baseSlug = Slugify(name);
        if (!taken.Contains(baseSlug, StringComparer.Ordinal))
        {
            return baseSlug;
        }

        for (var n = 2; ; n++)
        {
            var suffix = "-" + n.ToString(CultureInfo.InvariantCulture);
            var candidate = baseSlug[..Math.Min(baseSlug.Length, MaxLength - suffix.Length)].TrimEnd('-') + suffix;
            if (!taken.Contains(candidate, StringComparer.Ordinal))
            {
                return candidate;
            }
        }
    }

    private static string Slugify(string name)
    {
        var builder = new StringBuilder();
        foreach (var c in (name ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            var lower = char.ToLowerInvariant(c == 'ł' || c == 'Ł' ? 'l' : c);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(lower);
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && (builder.Length == 0 || builder[^1] != '-'))
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString().Trim('-');
        slug = slug.Length > MaxLength ? slug[..MaxLength].TrimEnd('-') : slug;
        return slug.Length == 0 ? Fallback : slug;
    }
}
