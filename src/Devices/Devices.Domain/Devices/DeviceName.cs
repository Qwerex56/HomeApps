using System.Globalization;
using FluentResults;

namespace Devices.Domain.Devices;

/// <summary>
/// The display name of a device: any text of up to 60 characters (Polish letters, spaces, emoji), unique per household
/// (per person for a computer) regardless of case. The stable key used in tool names and the API is the <see cref="DeviceSlug"/>.
/// </summary>
public static class DeviceName
{
    public const int MaxLength = 60;

    public static Result<string> Normalize(string? name)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length == 0 || new StringInfo(name).LengthInTextElements > MaxLength || name.Any(char.IsControl))
        {
            return Result.Fail(DeviceErrors.InvalidName);
        }

        return name;
    }
}
