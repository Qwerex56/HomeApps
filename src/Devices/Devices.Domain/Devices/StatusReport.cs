using FluentResults;

namespace Devices.Domain.Devices;

/// <summary>
/// A device's heartbeat (<c>status</c> frame): every field is optional, for what the device does not know.
/// <see cref="Battery"/> is a percentage, <see cref="Voltage"/> in volts, <see cref="Rssi"/> in dBm, <see cref="Uptime"/> in seconds.
/// </summary>
public sealed record StatusReport(double? Battery, double? Voltage, int? Rssi, long? Uptime, string? Firmware, string? Error)
{
    public const int MaxFirmwareLength = 50;
    public const int MaxErrorLength = 200;

    public Result Check()
    {
        var problems = new List<string>();
        if (Battery is < 0 or > 100)
        {
            problems.Add("battery is a percentage from 0 to 100");
        }

        if (Voltage is < 0 or > 100)
        {
            problems.Add("voltage is in volts, from 0 to 100");
        }

        if (Rssi is < -200 or > 0)
        {
            problems.Add("rssi is in dBm, from -200 to 0");
        }

        if (Uptime is < 0)
        {
            problems.Add("uptime is a number of seconds, not negative");
        }

        if (Firmware is { Length: > MaxFirmwareLength })
        {
            problems.Add($"firmware is at most {MaxFirmwareLength} characters");
        }

        if (Error is { Length: > MaxErrorLength })
        {
            problems.Add($"error is at most {MaxErrorLength} characters");
        }

        return problems.Count == 0 ? Result.Ok() : Result.Fail(DeviceErrors.InvalidFrame(problems));
    }
}
