using MacDeviceEmulator.Abstractions;

namespace MacDeviceEmulator.Services;

/// <summary>
/// Local "is this for me?" gate evaluated entirely on the computer before anything leaves the machine.
/// </summary>
public sealed class LocalUtteranceGate : IUtteranceGate
{
    private static readonly string[] DefaultWakeWords =
    [
        "hej jarvisie",
        "słuchaj jarvisie",
        "hej jarvis",
        "hey jarvis",
        "ok jarvis",
        "okay jarvis",
        "cześć jarvis",
        "słuchaj jarvis",
        "jarvisie",
        "jarvis"
    ];

    private static readonly HashSet<string> ThirdPersonPrepositions = new(StringComparer.OrdinalIgnoreCase)
    {
        "to", "for", "about", "with", "from", "of", "by", "o", "dla", "do", "od", "z", "ze", "bez", "przez", "u", "przy"
    };

    private static readonly char[] PunctuationAndSpace =
        [' ', '\t', '\r', '\n', ',', '.', ':', ';', '!', '?', '-', '–', '—', '…', '"', '\''];

    private readonly IReadOnlyList<string> _wakeWords;

    public LocalUtteranceGate(IEnumerable<string>? wakeWords = null)
    {
        var source = wakeWords?
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _wakeWords = (source is { Length: > 0 } ? source : DefaultWakeWords)
            .OrderByDescending(w => w.Length)
            .ToArray();
    }

    public GateDecision Evaluate(string? transcript, GateContext context)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return Rejected(GateRejectionReason.Empty);
        }

        if (context.IsSessionLocked)
        {
            return Rejected(GateRejectionReason.SessionLocked);
        }

        if (context.IsMuted)
        {
            return Rejected(GateRejectionReason.Muted);
        }

        if (context.IsEcho)
        {
            return Rejected(GateRejectionReason.EchoSuppressed);
        }

        var trimmed = transcript.Trim().TrimStart(PunctuationAndSpace);
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Rejected(GateRejectionReason.Empty);
        }

        if (!TryExtractAddressedCommand(trimmed, out var matchedWakeOnly, out var command))
        {
            return Rejected(GateRejectionReason.NotAddressedToMe);
        }

        if (matchedWakeOnly || string.IsNullOrWhiteSpace(command))
        {
            return Rejected(GateRejectionReason.NoCommandAfterWakeWord);
        }

        if (context.HasPendingConfirmations && VoiceConsentDetector.IsConsentPhrase(command))
        {
            return Rejected(GateRejectionReason.VoiceConfirmationBlocked);
        }

        return new GateDecision(true, command, GateRejectionReason.None);
    }

    private bool TryExtractAddressedCommand(string input, out bool matchedWakeOnly, out string command)
    {
        matchedWakeOnly = false;
        command = string.Empty;

        // 1. Prefix addressing: "Jarvis, włącz światło" / "Hej Jarvis: która godzina?"
        foreach (var wake in _wakeWords)
        {
            if (!input.StartsWith(wake, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (input.Length == wake.Length)
            {
                matchedWakeOnly = true;
                return true;
            }

            var nextChar = input[wake.Length];
            if (!char.IsWhiteSpace(nextChar) && !IsSeparatorPunctuation(nextChar))
            {
                continue;
            }

            var rest = input[wake.Length..].TrimStart(PunctuationAndSpace).Trim();
            if (rest.Length == 0)
            {
                matchedWakeOnly = true;
                return true;
            }

            command = rest;
            return true;
        }

        // 2. Suffix vocative addressing: "Włącz lampę, Jarvis" / "Która godzina Jarvis?"
        var withoutTrailingPunct = input.TrimEnd('.', '!', '?', '…', ' ', '\t');
        foreach (var wake in _wakeWords)
        {
            if (!withoutTrailingPunct.EndsWith(wake, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var prefixEnd = withoutTrailingPunct.Length - wake.Length;
            if (prefixEnd <= 0)
            {
                continue;
            }

            var prevChar = withoutTrailingPunct[prefixEnd - 1];
            if (!char.IsWhiteSpace(prevChar) && !IsSeparatorPunctuation(prevChar))
            {
                continue;
            }

            var prefix = withoutTrailingPunct[..prefixEnd].TrimEnd(PunctuationAndSpace).Trim();
            if (prefix.Length == 0)
            {
                matchedWakeOnly = true;
                return true;
            }

            var words = prefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length is 0 or > 12)
            {
                continue;
            }

            if (ThirdPersonPrepositions.Contains(words[^1]))
            {
                continue;
            }

            command = prefix;
            return true;
        }

        return false;
    }

    private static bool IsSeparatorPunctuation(char c) =>
        c is ',' or ':' or ';' or '.' or '!' or '?' or '-' or '–' or '—' or '…';

    private static GateDecision Rejected(GateRejectionReason reason) =>
        new(false, null, reason);
}
