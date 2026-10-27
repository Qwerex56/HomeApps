using Households.Domain.Common;
using FluentResults;

namespace Households.Domain.Households;

/// <summary>
/// A member's name inside one household: 1–32 characters. Optional (Q4): without it the household shows the display
/// name from Accounts, so a rename there shows up everywhere.
/// </summary>
public sealed record Nickname
{
    public const int MaxLength = 32;

    private Nickname(string value) => Value = value;

    public string Value { get; }

    /// <summary>A blank input clears the nickname and succeeds with null.</summary>
    public static Result<Nickname?> Create(string? raw)
    {
        var value = Text.OrNull(raw);
        if (value is null)
        {
            return Result.Ok<Nickname?>(null);
        }

        if (value.Length > MaxLength || Text.HasControlCharacters(value))
        {
            return Result.Fail<Nickname?>(HouseholdErrors.NicknameInvalid);
        }

        return new Nickname(value);
    }

    public override string ToString() => Value;
}
