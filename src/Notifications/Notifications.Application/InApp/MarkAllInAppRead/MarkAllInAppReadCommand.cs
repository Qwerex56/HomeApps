using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.InApp.MarkAllInAppRead;

/// <summary>2.13: all my unread notifications were read.</summary>
public sealed record MarkAllInAppReadCommand : ICommand;
