namespace Jarvis.Application.Voice;

/// <summary>
/// "Tak" or "nie" right after JARVIS asks to confirm (jarvis-voice-bridge.md, V6). Only a short answer made of these
/// words counts; anything else ("nie wiem, co kupić") is a new request.
/// </summary>
public static class Consent
{
    private static readonly HashSet<string> Yes = ["tak", "potwierdzam", "potwierdź", "zrób", "zgoda", "jasne", "ok", "okej", "dobrze", "śmiało", "dawaj"];
    private static readonly HashSet<string> No = ["nie", "anuluj", "odwołaj", "stop", "zostaw", "rezygnuję"];
    private static readonly HashSet<string> Filler = ["to", "tego", "rób", "proszę", "jarvis", "jarvisie", "już"];

    /// <returns><c>true</c> to confirm, <c>false</c> to cancel, <c>null</c> when it is not an answer.</returns>
    public static bool? Of(string transcript)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        var words = transcript.ToLowerInvariant()
            .Split([' ', ',', '.', '!', '?', ';', ':', '-', '…'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length is 0 or > 5 || words.Any(w => !Yes.Contains(w) && !No.Contains(w) && !Filler.Contains(w)))
        {
            return null;
        }

        var no = words.Any(No.Contains);
        var yes = words.Any(Yes.Contains);
        return no ? false : yes ? true : null;
    }
}
