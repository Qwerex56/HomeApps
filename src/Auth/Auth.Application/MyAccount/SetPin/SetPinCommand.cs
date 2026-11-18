using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.MyAccount.SetPin;

/// <summary>
/// jarvis-satellites.md S4: my spoken PIN for household satellites ("tu Adrian, PIN 1234"), 4–8 digits. Needs the
/// current password, or for an account without one a sign-in within the last 5 minutes (R8). Replaces an older PIN.
/// </summary>
public sealed record SetPinCommand(string Pin, string? CurrentPassword) : ICommand;
