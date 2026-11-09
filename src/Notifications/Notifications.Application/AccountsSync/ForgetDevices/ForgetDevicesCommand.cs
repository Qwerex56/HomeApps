using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.AccountsSync.ForgetDevices;

/// <summary>Accounts' <c>UserDisabled</c>: a disabled account gets nothing; the person turns notifications on again after coming back.</summary>
public sealed record ForgetDevicesCommand(Guid UserId) : ICommand;
