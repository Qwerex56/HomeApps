using Households.Domain.Common;
using FluentResults;

namespace Households.Domain.Households;

/// <summary>The household's name: 1–32 characters, trimmed, no control characters.</summary>
public sealed record HouseholdName
{
    public const int MaxLength = 32;

    private HouseholdName(string value) => Value = value;

    public string Value { get; }

    public static Result<HouseholdName> Create(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length is 0 or > MaxLength || Text.HasControlCharacters(value))
        {
            return Result.Fail<HouseholdName>(HouseholdErrors.NameInvalid);
        }

        return new HouseholdName(value);
    }

    public override string ToString() => Value;
}
