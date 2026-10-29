using Households.Domain.Common;
using FluentResults;

namespace Households.Domain.Households;

/// <summary>Optional description: up to 120 characters, trimmed, no control characters except new lines.</summary>
public sealed record HouseholdDescription
{
    public const int MaxLength = 120;

    private HouseholdDescription(string value) => Value = value;

    public string Value { get; }

    /// <summary>A blank input means "no description" and succeeds with null.</summary>
    public static Result<HouseholdDescription?> Create(string? raw)
    {
        var value = Text.OrNull(raw?.Replace("\r\n", "\n", StringComparison.Ordinal));
        if (value is null)
        {
            return Result.Ok<HouseholdDescription?>(null);
        }

        if (value.Length > MaxLength || value.Any(c => char.IsControl(c) && c != '\n'))
        {
            return Result.Fail<HouseholdDescription?>(HouseholdErrors.DescriptionInvalid);
        }

        return new HouseholdDescription(value);
    }

    public override string ToString() => Value;
}
