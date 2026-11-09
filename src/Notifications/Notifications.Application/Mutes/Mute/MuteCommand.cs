using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.Mutes.Mute;

/// <summary>Nothing more about this household, or with <paramref name="Source"/> from that plugin in it.</summary>
public sealed record MuteCommand(Guid HouseholdId, string? Source) : ICommand;
