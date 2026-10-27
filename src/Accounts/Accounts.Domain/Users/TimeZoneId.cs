using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>The user's time zone as an IANA id (<c>Europe/Warsaw</c>), for reminders and schedules.</summary>
public sealed record TimeZoneId
{
    /// <summary>Q3: <c>IdentityRegistered</c> carries no zone, so every account starts here.</summary>
    public static readonly TimeZoneId Default = new("Europe/Warsaw");

    private TimeZoneId(string value) => Value = value;

    public string Value { get; }

    public static Result<TimeZoneId> Create(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length == 0
            || !TimeZoneInfo.TryFindSystemTimeZoneById(value, out var zone)
            || !zone.HasIanaId)
        {
            return Result.Fail<TimeZoneId>(UserErrors.TimeZoneInvalid);
        }

        return new TimeZoneId(zone.Id);
    }

    public override string ToString() => Value;
}
