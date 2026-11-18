namespace Jarvis.Application.Voice;

public enum SpeakerOutcome
{
    /// <summary>One of the household's people has this PIN.</summary>
    Known,

    /// <summary>Nobody has it; Auth counts the miss.</summary>
    Unknown,

    /// <summary>Two people share it and no name narrowed it down.</summary>
    Ambiguous,

    /// <summary>Too many misses: the satellite waits a quarter of an hour.</summary>
    Locked,
}
