using Jarvis.VoiceBridge;

// JARVIS's ears and mouth on the Mac (jarvis-voice-bridge.md, docs/jarvis.md "Voice bridge").
//   dotnet run --project src/Jarvis/Jarvis.VoiceBridge -- [--server https://home-apps.dev] [--device mac-biuro]
//     [--threshold 0.5] [--insecure] [--save-recordings ~/jarvis-nagrania] [--check-wake plik.wav] [--logout]
var options = BridgeOptions.Parse(args);
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stop.Cancel();
};

using var http = new HttpClient(new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, errors) => options.AcceptsCertificate(errors),
});
var login = new OAuthLogin(http, options, new TokenStore(options.Server));
if (options.Logout)
{
    login.Forget();
    Console.WriteLine("Zapomniałem logowanie; odłącz też aplikację w „Połączonych aplikacjach”.");
    return;
}

using var wake = new WakeWord(await ModelFiles.EnsureAsync(http, stop.Token));
if (options.CheckWake is { } file)
{
    var (samples, rate) = Wav.Read(await File.ReadAllBytesAsync(file, stop.Token));
    Console.WriteLine(rate == Microphone.SampleRate
        ? $"Najwyższy wynik „Hey Jarvis”: {wake.Scan(samples):0.000} (próg {options.Threshold})."
        : $"Plik musi mieć 16 kHz, ma {rate} Hz.");
    return;
}

using var microphone = new Microphone();
await using var link = new VoiceLink(options);
using var bridge = new Bridge(options, wake, login, link, microphone);
await bridge.RunAsync(stop.Token);
