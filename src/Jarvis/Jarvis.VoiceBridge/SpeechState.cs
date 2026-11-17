namespace Jarvis.VoiceBridge;

/// <summary>Where an utterance is, frame by frame (<see cref="EndOfSpeech"/>).</summary>
internal enum SpeechState
{
    /// <summary>Keep sending.</summary>
    Going,

    /// <summary>The person spoke and then was quiet long enough: send <c>stop</c>.</summary>
    Ended,

    /// <summary>Nobody spoke after the chime: send <c>cancel</c>.</summary>
    NothingSaid,

    /// <summary>The longest utterance is reached: send <c>stop</c>.</summary>
    TooLong,
}
