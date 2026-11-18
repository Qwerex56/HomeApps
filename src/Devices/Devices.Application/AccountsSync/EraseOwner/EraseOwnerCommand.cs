using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.AccountsSync.EraseOwner;

/// <summary>Accounts' <c>UserErased</c> ("forget me"): the person's devices go, with their actions.</summary>
public sealed record EraseOwnerCommand(Guid UserId) : ICommand;
