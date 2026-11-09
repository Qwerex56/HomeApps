using FluentResults;

namespace Notifications.Domain.Recipients;

/// <summary>
/// N3: "do not disturb" every day from <see cref="From"/> to <see cref="To"/> in the person's own time zone (IANA,
/// from the browser). When <see cref="From"/> is later than <see cref="To"/> the hours run over midnight (22:00–07:00).
/// </summary>
public sealed record QuietHours(TimeOnly From, TimeOnly To, string TimeZone)
{
    public static Result<QuietHours> Create(TimeOnly from, TimeOnly to, string? timeZone)
    {
        if (from == to)
        {
            return Result.Fail(RecipientErrors.QuietHoursEmpty);
        }

        if (string.IsNullOrWhiteSpace(timeZone) || !TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
        {
            return Result.Fail(RecipientErrors.UnknownTimeZone);
        }

        return new QuietHours(from, to, timeZone);
    }

    public bool Covers(DateTimeOffset now)
    {
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(TimeZone, out var found) ? found : TimeZoneInfo.Utc;
        var local = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        return From < To ? local >= From && local < To : local >= From || local < To;
    }
}
