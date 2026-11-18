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
            var flat = char.IsControl(c) || char.IsWhiteSpace(c) || c is '<' or '>' or '"' or '`' ? ' ' : c;
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
