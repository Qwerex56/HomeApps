namespace Devices.Domain.Devices;

/// <summary>
/// What the device is mainly for (household-jarvis-devices.md 2.2); the device reports it in its descriptor when it pairs.
/// An assistant is JARVIS's ears and mouth, a smart-home device has state and controls, a computer is a person's own.
/// </summary>
public enum DevicePurpose
{
    Assistant,
    SmartHome,
    Computer,
}
