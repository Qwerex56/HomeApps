using System.Globalization;
using System.Text;

namespace Devices.Application.Common;

/// <summary>
/// Text a device or an admin wrote (the <c>jarvis</c> field, notes, names) before it goes next to JARVIS's instructions: it is data, never a
/// command, so control characters, line breaks, angle brackets and quotes are flattened, runs of spaces collapse and the length is capped.
/// </summary>
internal static class DeviceText
{
    public static string Clean(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(text.Length, maxLength));
        foreach (var c in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format)
            {
                continue;
            }

            var flat = c switch
            {
                '\u201C' or '\u201D' or '\u201E' or '\u201F' or '\uFF02' or '\u2039' or '\u203A' or '\u00AB' or '\u00BB' => '\'',
                '\uFF1C' or '\uFF1E' or '\uFE64' or '\uFE65' => ' ',
                _ => char.IsControl(c) || char.IsWhiteSpace(c) || c is '<' or '>' or '"' or '`' ? ' ' : c,
            };
            if (flat == ' ' && (builder.Length == 0 || builder[^1] == ' '))
            {
                continue;
            }

            builder.Append(flat);
            if (builder.Length >= maxLength)
            {
                break;
            }
        }

        return builder.ToString().TrimEnd();
    }
}
