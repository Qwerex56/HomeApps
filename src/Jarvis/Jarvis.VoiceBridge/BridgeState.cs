namespace Jarvis.VoiceBridge;

/// <summary>What the bridge does with the microphone right now.</summary>
internal enum BridgeState
{
    /// <summary>Only the wake word hears; nothing is sent.</summary>
    Waiting,

    /// <summary>After "Hey Jarvis" (or Enter): the utterance goes to the server.</summary>
    Recording,

    /// <summary>JARVIS asked "tak czy nie?": the next answer goes without the wake word (V6).</summary>
    Answering,

    /// <summary>The server works or JARVIS speaks: the microphone is ignored, so he does not hear himself.</summary>
    Busy,
}
