namespace MacDeviceEmulator.Abstractions;

public sealed record GateContext(
    bool IsMuted,
    bool IsSessionLocked,
    bool IsEcho,
    bool HasPendingConfirmations);
