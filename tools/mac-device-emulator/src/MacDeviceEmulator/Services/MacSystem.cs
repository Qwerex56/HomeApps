using System.Globalization;
using System.Text.RegularExpressions;
using MacDeviceEmulator.Abstractions;

namespace MacDeviceEmulator.Services;

public sealed partial class MacSystem(IProcessRunner runner) : IMacSystem
{
    [GeneratedRegex(@"output volume:(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex VolumeRegex();

    [GeneratedRegex(@"output muted:(true|false)", RegexOptions.IgnoreCase)]
    private static partial Regex MutedRegex();

    [GeneratedRegex(@"(\d+)%", RegexOptions.IgnoreCase)]
    private static partial Regex BatteryRegex();

    [GeneratedRegex(@"CPU usage: ([\d\.,]+)% user, ([\d\.,]+)% sys", RegexOptions.IgnoreCase)]
    private static partial Regex CpuRegex();

    [GeneratedRegex(@"\{\s*([\d\.,]+)\s+([\d\.,]+)\s+([\d\.,]+)\s*\}")]
    private static partial Regex LoadAvgRegex();

    public async Task<int> GetVolumeAsync(CancellationToken cancellationToken = default)
    {
        var result = await runner.RunAsync("osascript", ["-e", "output volume of (get volume settings)"], cancellationToken);
        if (result.ExitCode == 0 && int.TryParse(result.StandardOutput.Trim(), CultureInfo.InvariantCulture, out var volume))
        {
            return volume;
        }

        var fullResult = await runner.RunAsync("osascript", ["-e", "get volume settings"], cancellationToken);
        if (fullResult.ExitCode == 0)
        {
            var match = VolumeRegex().Match(fullResult.StandardOutput);
            if (match.Success && int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return 50;
    }

    public async Task SetVolumeAsync(int volume, CancellationToken cancellationToken = default)
    {
        var clamped = Math.Clamp(volume, 0, 100);
        await runner.RunAsync("osascript", ["-e", $"set volume output volume {clamped}"], cancellationToken);
    }

    public async Task<bool> IsMutedAsync(CancellationToken cancellationToken = default)
    {
        var result = await runner.RunAsync("osascript", ["-e", "output muted of (get volume settings)"], cancellationToken);
        if (result.ExitCode == 0 && bool.TryParse(result.StandardOutput.Trim(), out var muted))
        {
            return muted;
        }

        var fullResult = await runner.RunAsync("osascript", ["-e", "get volume settings"], cancellationToken);
        if (fullResult.ExitCode == 0)
        {
            var match = MutedRegex().Match(fullResult.StandardOutput);
            if (match.Success && bool.TryParse(match.Groups[1].Value, out var parsed))
            {
                return parsed;
            }
        }

        return false;
    }

    public async Task SetMutedAsync(bool muted, CancellationToken cancellationToken = default)
    {
        var flag = muted ? "true" : "false";
        await runner.RunAsync("osascript", ["-e", $"set volume output muted {flag}"], cancellationToken);
    }

    public async Task<double> GetCpuLoadAsync(CancellationToken cancellationToken = default)
    {
        var result = await runner.RunAsync("top", ["-l", "1", "-n", "0"], cancellationToken);
        if (result.ExitCode == 0)
        {
            var match = CpuRegex().Match(result.StandardOutput);
            if (match.Success)
            {
                var userStr = match.Groups[1].Value.Replace(',', '.');
                var sysStr = match.Groups[2].Value.Replace(',', '.');
                if (double.TryParse(userStr, CultureInfo.InvariantCulture, out var user) &&
                    double.TryParse(sysStr, CultureInfo.InvariantCulture, out var sys))
                {
                    return Math.Round(user + sys, 1);
                }
            }
        }

        var sysctl = await runner.RunAsync("sysctl", ["-n", "vm.loadavg"], cancellationToken);
        if (sysctl.ExitCode == 0)
        {
            var match = LoadAvgRegex().Match(sysctl.StandardOutput);
            if (match.Success)
            {
                var oneMinStr = match.Groups[1].Value.Replace(',', '.');
                if (double.TryParse(oneMinStr, CultureInfo.InvariantCulture, out var load))
                {
                    return Math.Round(load, 2);
                }
            }
        }

        return 0.0;
    }

    public async Task<int?> GetBatteryLevelAsync(CancellationToken cancellationToken = default)
    {
        var result = await runner.RunAsync("pmset", ["-g", "batt"], cancellationToken);
        if (result.ExitCode == 0)
        {
            var match = BatteryRegex().Match(result.StandardOutput);
            if (match.Success && int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var battery))
            {
                return battery;
            }
        }

        return null;
    }

    public async Task SayAsync(string text, CancellationToken cancellationToken = default)
    {
        await runner.RunAsync("say", ["--", text], cancellationToken);
    }
}
