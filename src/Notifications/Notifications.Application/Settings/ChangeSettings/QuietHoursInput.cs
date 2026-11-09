namespace Notifications.Application.Settings.ChangeSettings;

/// <param name="From">"22:00" (HH:mm).</param>
/// <param name="To">"07:00" (HH:mm).</param>
/// <param name="TimeZone">IANA, from the browser: <c>Intl.DateTimeFormat().resolvedOptions().timeZone</c>.</param>
public sealed record QuietHoursInput(string From, string To, string TimeZone);
