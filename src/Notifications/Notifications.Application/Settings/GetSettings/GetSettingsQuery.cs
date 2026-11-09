using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.Settings.GetSettings;

/// <summary>My notification settings, mutes and devices; defaults when I never set anything.</summary>
public sealed record GetSettingsQuery : IQuery<NotificationSettingsView>;
