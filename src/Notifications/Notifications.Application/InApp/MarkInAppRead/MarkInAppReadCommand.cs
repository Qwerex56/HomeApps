using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.InApp.MarkInAppRead;

/// <summary>2.13: one of my notifications was read. Already read: nothing changes. Not mine: 404.</summary>
public sealed record MarkInAppReadCommand(Guid Id) : ICommand;
