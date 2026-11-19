namespace Jarvis.Application.Voice;

/// <summary>
/// JARVIS's ears and voice (section <c>Jarvis:Voice</c>, jarvis-voice-bridge.md). Both engines speak the OpenAI audio
/// API (<c>/v1/audio/transcriptions</c>, <c>/v1/audio/speech</c>), so Speaches on the Mac, a box with a GPU or a cloud
/// service is only a different URL. Off by default: a server without a speech engine works as before.
/// </summary>
public sealed class VoiceOptions
{
    public const string SectionName = "Jarvis:Voice";

    public bool Enabled { get; set; }

    /// <summary>Base URL of the speech-to-text engine, e.g. <c>http://mac.local:8000</c>.</summary>
    public string SttUrl { get; set; } = string.Empty;

    public string SttModel { get; set; } = "deepdml/faster-whisper-large-v3-turbo-ct2";

    /// <summary>Base URL of the text-to-speech engine; usually the same Speaches as <see cref="SttUrl"/>.</summary>
    public string TtsUrl { get; set; } = string.Empty;

    public string TtsModel { get; set; } = "speaches-ai/piper-pl_PL-gosia-medium";

    public string TtsVoice { get; set; } = "gosia";

    public string Language { get; set; } = "pl";

    /// <summary>Only for a cloud engine; Speaches at home needs none.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// How long a household satellite keeps answering as the person who said their PIN, counted from their last turn
    /// (S4: "a few minutes"); then it speaks for itself again.
    /// </summary>
    public int PersonMinutes { get; set; } = 3;

    /// <summary>The longest utterance kept; the rest is dropped.</summary>
    public int MaxSeconds { get; set; } = 30;
}
