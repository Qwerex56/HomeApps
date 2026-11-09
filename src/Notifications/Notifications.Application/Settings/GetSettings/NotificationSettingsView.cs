namespace Notifications.Application.Settings.GetSettings;

public sealed record NotificationSettingsView(
    bool ShowPreview,
    QuietHoursView? QuietHours,
    IReadOnlyList<MuteView> Mutes,
    IReadOnlyList<DeviceView> Devices);
