using System.Globalization;

namespace Jarvis.VoiceBridge;

/// <summary>What the bridge is started with; every option also has a default, so <c>dotnet run</c> alone works.</summary>
/// <param name="Server">HomeApps' address (<c>--server</c> or <c>HOMEAPPS_URL</c>), e.g. https://home-apps.dev.</param>
/// <param name="Device">How this listener is called on the server, e.g. "mac-biuro".</param>
/// <param name="Threshold">The wake word's score from 0 to 1 that counts as "Hey Jarvis".</param>
/// <param name="Insecure">Accept the dev certificate of https://app.localhost.</param>
/// <param name="SaveRecordings">V7: a folder where each turn's audio is kept, only on this computer.</param>
/// <param name="CheckWake">A WAV file to score with the wake word, then exit (to tune the threshold).</param>
internal sealed record BridgeOptions(
    Uri Server, string Device, float Threshold, bool Insecure, string? SaveRecordings, string? CheckWake, bool Logout)
{
    /// <summary>Models, the app's registration and (outside macOS) the token.</summary>
    public static string Home { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".homeapps", "voice-bridge");

    /// <summary><c>--insecure</c> lets through only the self-signed certificate of a local dev server.</summary>
    public bool AcceptsCertificate(System.Net.Security.SslPolicyErrors errors) =>
        errors == System.Net.Security.SslPolicyErrors.None
        || (Insecure && Server.Host.EndsWith("localhost", StringComparison.OrdinalIgnoreCase));

    public static BridgeOptions Parse(string[] args)
    {
        string? Value(string name)
        {
            var at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        var server = Value("--server") ?? Environment.GetEnvironmentVariable("HOMEAPPS_URL") ?? "https://home-apps.dev";
        return new BridgeOptions(
            new Uri(server.TrimEnd('/') + "/"),
            Value("--device") ?? Environment.MachineName,
            float.Parse(Value("--threshold") ?? "0.5", CultureInfo.InvariantCulture),
            args.Contains("--insecure"),
            Value("--save-recordings"),
            Value("--check-wake"),
            args.Contains("--logout"));
    }
}
