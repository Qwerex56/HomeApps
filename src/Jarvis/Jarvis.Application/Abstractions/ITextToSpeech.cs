namespace Jarvis.Application.Abstractions;

/// <summary>Text to speech (jarvis-voice-bridge.md 3); which engine and voice is configuration, not code.</summary>
public interface ITextToSpeech
{
    /// <returns>A WAV file, so the listener reads the format from its header.</returns>
    Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken);
}
