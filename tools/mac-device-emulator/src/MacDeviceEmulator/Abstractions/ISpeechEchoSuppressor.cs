namespace MacDeviceEmulator.Abstractions;

/// <summary>Tracks JARVIS spoken output to suppress self-voice echo in continuous listening.</summary>
public interface ISpeechEchoSuppressor
{
    bool IsSuppressing { get; }

    IDisposable BeginSpeaking(string text);

    Task TrackSpeakingAsync(string text, Func<CancellationToken, Task> speakAction, CancellationToken cancellationToken = default);

    bool IsEcho(string heardText);
}
