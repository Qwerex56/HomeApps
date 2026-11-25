using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.InApp.ListInApp;

/// <summary>2.13: my notification center, newest first; <paramref name="Limit"/> defaults to 50, at most 200.</summary>
public sealed record ListInAppQuery(int Limit = 50, bool UnreadOnly = false) : IQuery<NotificationListDto>;
