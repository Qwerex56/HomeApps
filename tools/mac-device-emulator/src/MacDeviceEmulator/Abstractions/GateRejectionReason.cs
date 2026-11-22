namespace MacDeviceEmulator.Abstractions;

public enum GateRejectionReason
{
    None = 0,
    Empty = 1,
    Muted = 2,
    SessionLocked = 3,
    EchoSuppressed = 4,
    NotAddressedToMe = 5,
    NoCommandAfterWakeWord = 6,
    VoiceConfirmationBlocked = 7
}
