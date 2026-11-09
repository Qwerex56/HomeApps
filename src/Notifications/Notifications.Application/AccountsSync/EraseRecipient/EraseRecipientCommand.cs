using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.AccountsSync.EraseRecipient;

/// <summary>Accounts' <c>UserErased</c> ("forget me"): devices, settings and mutes go.</summary>
public sealed record EraseRecipientCommand(Guid UserId) : ICommand;
