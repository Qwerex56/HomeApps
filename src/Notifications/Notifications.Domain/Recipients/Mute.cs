using FluentResults;

namespace Notifications.Domain.Recipients;

/// <summary>Nothing about <see cref="HouseholdId"/> reaches the person; with <see cref="Source"/> only from that plugin or service.</summary>
public sealed record Mute(Guid HouseholdId, string? Source)
{
    public const int MaxSourceLength = 32;

    public static Result<Mute> Create(Guid householdId, string? source)
    {
        if (householdId == Guid.Empty)
        {
            return Result.Fail(RecipientErrors.InvalidMute);
        }

        var trimmed = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        if (trimmed is { Length: > MaxSourceLength })
        {
            return Result.Fail(RecipientErrors.InvalidMute);
        }

        return new Mute(householdId, trimmed);
    }
}
