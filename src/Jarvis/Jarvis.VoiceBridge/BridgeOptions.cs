using System.Globalization;

namespace Jarvis.VoiceBridge;

/// <summary>What the bridge is started with; every option also has a default, so <c>dotnet run</c> alone works.</summary>
/// <param name="Server">HomeApps' address (<c>--server</c> or <c>HOMEAPPS_URL</c>), e.g. https://home-apps.dev.</param>
/// <param name="Device">How this listener is called on the server, e.g. "mac-biuro".</param>
/// <param name="Threshold">The wake word's score from 0 to 1 that counts as "Hey Jarvis".</param>
/// <param name="Insecure">Accept the dev certificate of https://app.localhost.</param>
/// <param name="SaveRecordings">V7: a folder where each turn's audio is kept, only on this computer.</param>
/// <param name="CheckWake">A WAV file to score with the wake word, then exit (to tune the threshold).</param>
/// <param name="Engine">S1: with <c>--speech local</c> the bridge's own speech engine (OpenAI audio API, e.g. Speaches on
/// this Mac); the server then only gets and gives text. <c>null</c>: the server hears and speaks.</param>
/// <param name="Actions">A JSON file with what this computer offers JARVIS (jarvis-satellites.md, S-0); <c>null</c>: nothing.</param>
/// <param name="Pair">S-1e: a pairing code from the household's Urządzenia page; the bridge becomes that household's satellite.</param>
/// <param name="Unpair">Forget the satellite's pairing on this computer; the bridge is the person's again.</param>
internal sealed record BridgeOptions(
    Uri Server,
    string Device,
    float Threshold,
    bool Insecure,
    string? SaveRecordings,
    string? CheckWake,
    bool Logout,
    Uri? Engine = null,
    string? Actions = null,
    string? Pair = null,
    bool Unpair = false)
{
    public const string SttModel = "deepdml/faster-whisper-large-v3-turbo-ct2";
    public const string TtsModel = "speaches-ai/piper-pl_PL-gosia-medium";
    public const string TtsVoice = "gosia";

    public bool LocalSpeech => Engine is not null;

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
        var engine = Value("--speech") == "local" ? Value("--engine") ?? "http://localhost:8000" : null;
        return new BridgeOptions(
            new Uri(server.TrimEnd('/') + "/"),
            Value("--device") ?? Environment.MachineName,
            float.Parse(Value("--threshold") ?? "0.5", CultureInfo.InvariantCulture),
            args.Contains("--insecure"),
            Value("--save-recordings"),
            Value("--check-wake"),
            args.Contains("--logout"),
            engine is null ? null : new Uri(engine.TrimEnd('/') + "/"),
            Value("--actions"),
            Value("--pair"),
            args.Contains("--unpair"));
    }
}
