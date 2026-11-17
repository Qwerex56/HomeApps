using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Voice;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure.Voice;

/// <summary><c>POST /v1/audio/transcriptions</c>, the OpenAI audio API that Speaches, whisper servers and OpenAI share.</summary>
internal sealed class OpenAiSpeechToText(HttpClient http, IOptions<VoiceOptions> options) : ISpeechToText
{
    public async Task<string> TranscribeAsync(ReadOnlyMemory<byte> pcm, int sampleRate, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var file = new ByteArrayContent(Wav.FromPcm16(pcm.Span, sampleRate));
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        using var form = new MultipartFormDataContent
        {
            { file, "file", "turn.wav" },
            { new StringContent(settings.SttModel), "model" },
            { new StringContent(settings.Language), "language" },
            { new StringContent("json"), "response_format" },
        };

        using var response = await http.PostAsync("v1/audio/transcriptions", form, cancellationToken);
        response.EnsureSuccessStatusCode();
        var heard = await response.Content.ReadFromJsonAsync<Transcription>(cancellationToken);
        return heard?.Text ?? string.Empty;
    }

    private sealed record Transcription([property: JsonPropertyName("text")] string? Text);
}
