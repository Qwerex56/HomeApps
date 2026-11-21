using System.Text.RegularExpressions;
using MacDeviceEmulator.Abstractions;

namespace MacDeviceEmulator.Services;

/// <summary>Checks macOS console session lock state via <c>ioreg</c>.</summary>
public sealed partial class MacSessionLockMonitor(IProcessRunner runner) : ISessionLockMonitor
{
    [GeneratedRegex(@"""?CGSSessionScreenIsLocked""?\s*=\s*(Yes|true|1)", RegexOptions.IgnoreCase)]
    private static partial Regex ScreenLockedRegex();

    [GeneratedRegex(@"""?kCGSSessionOnConsoleKey""?\s*=\s*(No|false|0)", RegexOptions.IgnoreCase)]
    private static partial Regex OffConsoleRegex();

    public async Task<bool> IsLockedAsync(CancellationToken cancellationToken = default)
    {
        var result = await runner.RunAsync("ioreg", ["-n", "Root", "-d1", "-w", "0"], cancellationToken);
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return false;
        }

        return ScreenLockedRegex().IsMatch(result.StandardOutput)
            || OffConsoleRegex().IsMatch(result.StandardOutput);
    }
}
