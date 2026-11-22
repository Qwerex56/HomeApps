namespace MacDeviceEmulator.Services;

/// <summary>
/// Detects short voice confirmation or cancellation phrases (matching server-side <c>Consent</c> rules)
/// so they can be blocked locally when confirmations must happen on screen only.
/// </summary>
public static class VoiceConsentDetector
{
    private static readonly HashSet<string> YesWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "tak", "potwierdzam", "potwierdź", "zrób", "zgoda", "jasne", "ok", "okej", "dobrze", "śmiało", "dawaj",
        "yes", "confirm", "approve"
    };

    private static readonly HashSet<string> NoWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "nie", "anuluj", "odwołaj", "stop", "zostaw", "rezygnuję",
        "no", "cancel", "deny"
    };

    private static readonly HashSet<string> FillerWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "to", "tego", "rób", "proszę", "jarvis", "jarvisie", "już", "please", "it"
    };

    public static bool IsConsentPhrase(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return false;
        }

        var words = transcript
            .Split([' ', ',', '.', '!', '?', ';', ':', '-', '…'], StringSplitOptions.RemoveEmptyEntries);

        if (words.Length is 0 or > 5)
        {
            return false;
        }

        if (words.Any(w => !YesWords.Contains(w) && !NoWords.Contains(w) && !FillerWords.Contains(w)))
        {
            return false;
        }

        return words.Any(YesWords.Contains) || words.Any(NoWords.Contains);
    }
}
