using Notifications.Application.Settings.ChangeSettings;

namespace Notifications.Api.Requests;

internal sealed record ChangeSettingsRequest(bool ShowPreview, QuietHoursInput? QuietHours);
