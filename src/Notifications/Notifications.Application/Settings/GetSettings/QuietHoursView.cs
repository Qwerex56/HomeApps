namespace Notifications.Application.Settings.GetSettings;

/// <param name="From">"22:00".</param>
/// <param name="To">"07:00".</param>
/// <param name="TimeZone">IANA, e.g. Europe/Warsaw.</param>
public sealed record QuietHoursView(string From, string To, string TimeZone);
