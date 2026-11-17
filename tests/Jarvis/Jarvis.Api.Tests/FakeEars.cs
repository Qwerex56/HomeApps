using System.Collections.Concurrent;
using Jarvis.Application.Abstractions;

namespace Jarvis.Api.Tests;

/// <summary>Speech to text without an engine: hears what the test queued, and remembers how much audio came.</summary>
internal sealed class FakeEars : ISpeechToText
{
    public ConcurrentQueue<string> Heard { get; } = new();

    public int LastBytes { get; private set; }

    public Task<string> TranscribeAsync(ReadOnlyMemory<byte> pcm, int sampleRate, CancellationToken cancellationToken)
    {
        LastBytes = pcm.Length;
        return Task.FromResult(Heard.TryDequeue(out var text) ? text : string.Empty);
    }
}
