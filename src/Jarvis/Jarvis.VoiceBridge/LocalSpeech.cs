using System.Net.Http.Json;
using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>
/// S1: the bridge's own ears and voice, the same OpenAI audio API JARVIS uses on the server (Speaches on this Mac by
/// default), so the server gets only text and needs no speech engine.
/// </summary>
internal sealed class LocalSpeech(HttpClient http, Uri engine)
{
    public async Task<string> TranscribeAsync(short[] pcm, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        var wav = new ByteArrayContent(Wav.Write(pcm, Microphone.SampleRate));
        wav.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        form.Add(wav, "file", "turn.wav");
        form.Add(new StringContent(BridgeOptions.SttModel), "model");
        form.Add(new StringContent("pl"), "language");
        form.Add(new StringContent("json"), "response_format");
        using var response = await http.PostAsync(new Uri(engine, "v1/audio/transcriptions"), form, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return json.TryGetProperty("text", out var text) ? text.GetString()?.Trim() ?? string.Empty : string.Empty;
    }

    public async Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(new Uri(engine, "v1/audio/speech"), new
        {
            model = BridgeOptions.TtsModel,
            voice = BridgeOptions.TtsVoice,
            input = text,
            response_format = "wav",
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
