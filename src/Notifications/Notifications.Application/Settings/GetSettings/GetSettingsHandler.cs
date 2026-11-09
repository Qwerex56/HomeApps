using System.Globalization;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Settings.GetSettings;

internal sealed class GetSettingsHandler(IRecipientRepository recipients, ICurrentUser currentUser)
    : IQueryHandler<GetSettingsQuery, NotificationSettingsView>
{
    public async Task<Result<NotificationSettingsView>> Handle(GetSettingsQuery query, CancellationToken cancellationToken)
    {
        var recipient = await recipients.FindAsync(currentUser.RequiredUserId, cancellationToken);
        if (recipient is null)
        {
            return new NotificationSettingsView(true, null, [], []);
        }

        var quiet = recipient.QuietHours is { } hours
            ? new QuietHoursView(Format(hours.From), Format(hours.To), hours.TimeZone)
            : null;
        return new NotificationSettingsView(
            recipient.ShowPreview,
            quiet,
            recipient.Mutes.Select(m => new MuteView(m.HouseholdId, m.Source)).ToList(),
            recipient.Devices
                .OrderByDescending(d => d.LastUsedAt ?? d.RegisteredAt)
                .Select(d => new DeviceView(d.Id, d.UserAgent, d.Endpoint, d.RegisteredAt, d.LastUsedAt))
                .ToList());
    }

    private static string Format(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);
}
