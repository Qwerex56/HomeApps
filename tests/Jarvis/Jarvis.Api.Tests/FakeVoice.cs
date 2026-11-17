using System.Text;
using Jarvis.Application.Abstractions;

namespace Jarvis.Api.Tests;

/// <summary>Text to speech without an engine: the "WAV" is the sentence itself, so a test can read what was said.</summary>
internal sealed class FakeVoice : ITextToSpeech
{
    public Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken) =>
        Task.FromResult(Encoding.UTF8.GetBytes(text));
}
