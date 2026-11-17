namespace Jarvis.Application.Abstractions;

/// <summary>Speech to text (jarvis-voice-bridge.md 3); which engine is configuration, not code.</summary>
public interface ISpeechToText
{
    /// <param name="pcm">16-bit little-endian mono samples.</param>
    Task<string> TranscribeAsync(ReadOnlyMemory<byte> pcm, int sampleRate, CancellationToken cancellationToken);
}
