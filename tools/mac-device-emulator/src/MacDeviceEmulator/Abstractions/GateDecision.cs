namespace MacDeviceEmulator.Abstractions;

public sealed record GateDecision(
    bool Accepted,
    string? CommandText,
    GateRejectionReason Reason);
