using System.Diagnostics;

namespace Jarvis.VoiceBridge;

/// <summary>
/// The app's registration and refresh token per server: in the macOS Keychain (<c>security</c>), elsewhere in a file
/// only the user can read under <see cref="BridgeOptions.Home"/>.
/// </summary>
internal sealed class TokenStore(Uri server)
{
    private const string Service = "homeapps-voice-bridge";

    public string? Read(string key)
    {
        if (OperatingSystem.IsMacOS())
        {
            var (code, output) = Security("find-generic-password", "-s", Service, "-a", Account(key), "-w");
            return code == 0 ? output.Trim() : null;
        }

        var file = File(key);
        return System.IO.File.Exists(file) ? System.IO.File.ReadAllText(file).Trim() : null;
    }

    public void Write(string key, string value)
    {
        if (OperatingSystem.IsMacOS())
        {
            Security("add-generic-password", "-U", "-s", Service, "-a", Account(key), "-w", value);
            return;
        }

        var file = File(key);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        System.IO.File.WriteAllText(file, value);
        if (!OperatingSystem.IsWindows())
        {
            System.IO.File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public void Delete(string key)
    {
        if (OperatingSystem.IsMacOS())
        {
            Security("delete-generic-password", "-s", Service, "-a", Account(key));
            return;
        }

        System.IO.File.Delete(File(key));
    }

    private string Account(string key) => $"{server.Authority}:{key}";

    private string File(string key) => Path.Combine(BridgeOptions.Home, "tokens", $"{server.Authority.Replace(':', '_')}.{key}");

    private static (int Code, string Output) Security(params string[] args)
    {
        var start = new ProcessStartInfo("security") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot run security.");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }
}
