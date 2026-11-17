using System.Net;
using System.Text;
using Jarvis.Application.Voice;
using Jarvis.Infrastructure.Voice;
using Microsoft.Extensions.Options;

namespace Jarvis.Api.Tests;

/// <summary>The OpenAI audio API as Speaches expects it: a WAV upload for the ears, JSON for the voice.</summary>
public sealed class SpeechEngineTests
{
    private static readonly IOptions<VoiceOptions> Options = Microsoft.Extensions.Options.Options.Create(new VoiceOptions());

    [Fact]
    public async Task The_ears_upload_the_audio_as_wav_with_model_and_language()
    {
        var engine = new Recorder("""{"text":" Dodaj mleko."}""");
        var ears = new OpenAiSpeechToText(engine.Client(), Options);

        var heard = await ears.TranscribeAsync(new byte[320], 16_000, CancellationToken.None);

        Assert.Equal(" Dodaj mleko.", heard);
        Assert.Equal("/v1/audio/transcriptions", engine.Path);
        Assert.Contains("RIFF", engine.Body, StringComparison.Ordinal);
        Assert.Contains("deepdml/faster-whisper-large-v3-turbo-ct2", engine.Body, StringComparison.Ordinal);
        Assert.Contains("name=language", engine.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_voice_asks_for_a_wav_in_the_configured_voice()
    {
        var engine = new Recorder("RIFF....");
        var voice = new OpenAiTextToSpeech(engine.Client(), Options);

        var wav = await voice.SpeakAsync("Cześć.", CancellationToken.None);

        Assert.Equal("RIFF....", Encoding.ASCII.GetString(wav));
        Assert.Equal("/v1/audio/speech", engine.Path);
        Assert.Contains("\"voice\":\"gosia\"", engine.Body, StringComparison.Ordinal);
        Assert.Contains("\"response_format\":\"wav\"", engine.Body, StringComparison.Ordinal);
    }

    private sealed class Recorder(string answer) : HttpMessageHandler
    {
        public string? Path { get; private set; }

        public string Body { get; private set; } = string.Empty;

        public HttpClient Client() => new(this) { BaseAddress = new Uri("http://speaches.test/") };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;
            Body = Encoding.UTF8.GetString(await request.Content!.ReadAsByteArrayAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(answer) };
        }
    }
}
