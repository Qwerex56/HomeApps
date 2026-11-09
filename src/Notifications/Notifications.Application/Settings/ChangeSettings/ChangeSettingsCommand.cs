using HomeApps.BuildingBlocks.Application.Messaging;

namespace Notifications.Application.Settings.ChangeSettings;

/// <summary>N3 and N4: content on the lock screen and quiet hours (<c>null</c> turns them off).</summary>
public sealed record ChangeSettingsCommand(bool ShowPreview, QuietHoursInput? QuietHours) : ICommand;
