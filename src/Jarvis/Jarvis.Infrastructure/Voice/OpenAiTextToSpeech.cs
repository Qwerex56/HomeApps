using System.Net.Http.Json;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Voice;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure.Voice;

/// <summary><c>POST /v1/audio/speech</c>, the OpenAI audio API that Speaches (Piper, Kokoro) and OpenAI share.</summary>
internal sealed class OpenAiTextToSpeech(HttpClient http, IOptions<VoiceOptions> options) : ITextToSpeech
{
    public async Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var response = await http.PostAsJsonAsync("v1/audio/speech", new
        {
            model = settings.TtsModel,
            voice = settings.TtsVoice,
            input = text,
            response_format = "wav",
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
