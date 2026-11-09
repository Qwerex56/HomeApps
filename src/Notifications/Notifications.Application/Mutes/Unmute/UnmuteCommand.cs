using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.Mutes.Unmute;

/// <summary>Takes back exactly the mute <c>MuteCommand</c> made; unmuting what was not muted is fine.</summary>
public sealed record UnmuteCommand(Guid HouseholdId, string? Source) : ICommand;
