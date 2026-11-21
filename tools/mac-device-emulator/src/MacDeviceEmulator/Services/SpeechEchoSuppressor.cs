using MacDeviceEmulator.Abstractions;

namespace MacDeviceEmulator.Services;

/// <summary>
/// Suppresses self-voice echo during active TTS playback, a short post-speech cooldown,
/// and a rolling spoken-text memory window for delayed STT transcripts.
/// </summary>
public sealed class SpeechEchoSuppressor : ISpeechEchoSuppressor
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _cooldown;
    private readonly TimeSpan _memoryWindow;
    private readonly Lock _gate = new();
    private readonly List<SpokenEntry> _recentUtterances = [];
    private int _activeSpeakers;
    private DateTimeOffset? _lastSpeechEndedAt;

    public SpeechEchoSuppressor(
        TimeProvider? timeProvider = null,
        TimeSpan? cooldown = null,
        TimeSpan? memoryWindow = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _cooldown = cooldown ?? TimeSpan.FromSeconds(2);
        _memoryWindow = memoryWindow ?? TimeSpan.FromSeconds(10);
    }

    public bool IsSuppressing
    {
        get
        {
            lock (_gate)
            {
                if (_activeSpeakers > 0)
                {
                    return true;
                }

                if (_lastSpeechEndedAt is { } endedAt && _timeProvider.GetUtcNow() - endedAt < _cooldown)
                {
                    return true;
                }

                return false;
            }
        }
    }

    public IDisposable BeginSpeaking(string text)
    {
        var now = _timeProvider.GetUtcNow();
        var normalized = Normalize(text);

        lock (_gate)
        {
            _activeSpeakers++;
            PruneLocked(now);
            if (!string.IsNullOrEmpty(normalized))
            {
                _recentUtterances.Add(new SpokenEntry(normalized, Tokenize(normalized), now));
            }
        }

        return new SpeakingScope(this);
    }

    public async Task TrackSpeakingAsync(string text, Func<CancellationToken, Task> speakAction, CancellationToken cancellationToken = default)
    {
        using var _ = BeginSpeaking(text);
        await speakAction(cancellationToken);
    }

    public bool IsEcho(string heardText)
    {
        if (string.IsNullOrWhiteSpace(heardText))
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        var normalizedHeard = Normalize(heardText);
        if (string.IsNullOrEmpty(normalizedHeard))
        {
            return false;
        }

        var heardWords = Tokenize(normalizedHeard);

        lock (_gate)
        {
            if (_activeSpeakers > 0)
            {
                return true;
            }

            if (_lastSpeechEndedAt is { } endedAt && now - endedAt < _cooldown)
            {
                return true;
            }

            PruneLocked(now);

            foreach (var entry in _recentUtterances)
            {
                if (normalizedHeard.Length >= 4 && entry.NormalizedText.Contains(normalizedHeard, StringComparison.Ordinal))
                {
                    return true;
                }

                if (heardWords.Length >= 2 && entry.Words.Length > 0)
                {
                    var matched = heardWords.Count(w => entry.Words.Contains(w, StringComparer.Ordinal));
                    var ratio = (double)matched / heardWords.Length;
                    if (ratio >= 0.6)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private void EndSpeaking()
    {
        var now = _timeProvider.GetUtcNow();
        lock (_gate)
        {
            if (_activeSpeakers > 0)
            {
                _activeSpeakers--;
            }

            _lastSpeechEndedAt = now;
        }
    }

    private void PruneLocked(DateTimeOffset now)
    {
        _recentUtterances.RemoveAll(e => now - e.At > _memoryWindow);
    }

    private static string Normalize(string text)
    {
        var chars = text.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) ? c : ' ')
            .ToArray();
        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string[] Tokenize(string normalized) =>
        normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private sealed record SpokenEntry(string NormalizedText, string[] Words, DateTimeOffset At);

    private sealed class SpeakingScope(SpeechEchoSuppressor owner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.EndSpeaking();
            }
        }
    }
}
