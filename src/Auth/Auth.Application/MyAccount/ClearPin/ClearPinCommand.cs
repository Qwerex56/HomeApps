using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.MyAccount.ClearPin;

/// <summary>S4: no PIN any more; satellites no longer answer as me.</summary>
public sealed record ClearPinCommand : ICommand;
