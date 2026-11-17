namespace Jarvis.VoiceBridge;

/// <summary>
/// When the person stopped talking, from the loudness of 80 ms frames: speech is clearly above the room's noise
/// (learnt while quiet), and the utterance ends after <see cref="Silence"/> of quiet once something was said.
/// Simple on purpose (jarvis-voice-bridge.md, section 1); Silero VAD can replace it later.
/// </summary>
internal sealed class EndOfSpeech(TimeSpan waitForSpeech)
{
    public static readonly TimeSpan Silence = TimeSpan.FromMilliseconds(700);
    public static readonly TimeSpan Longest = TimeSpan.FromSeconds(15);

    private const double FrameMs = 1000.0 * WakeWord.FrameSamples / 16_000;
    private const double MinimumSpeech = 500;

    private double _noise = 150;
    private double _quietMs;
    private double _elapsedMs;

    public bool Heard { get; private set; }

    public SpeechState Push(short[] frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _elapsedMs += FrameMs;
        var level = Rms(frame);
        var speech = level > Math.Max(MinimumSpeech, _noise * 3);
        if (speech)
        {
            Heard = true;
            _quietMs = 0;
        }
        else
        {
            _noise = (_noise * 0.9) + (level * 0.1);
            _quietMs += FrameMs;
        }

        if (_elapsedMs >= Longest.TotalMilliseconds)
        {
            return SpeechState.TooLong;
        }

        if (!Heard)
        {
            return _elapsedMs >= waitForSpeech.TotalMilliseconds ? SpeechState.NothingSaid : SpeechState.Going;
        }

        return _quietMs >= Silence.TotalMilliseconds ? SpeechState.Ended : SpeechState.Going;
    }

    private static double Rms(short[] frame)
    {
        double sum = 0;
        foreach (var sample in frame)
        {
            sum += (double)sample * sample;
        }

        return Math.Sqrt(sum / Math.Max(1, frame.Length));
    }
}
