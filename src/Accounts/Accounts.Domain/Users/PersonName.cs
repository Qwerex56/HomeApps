using Accounts.Domain.Common;
using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>An optional part of a person's name (first, last or preferred): at most 64 characters, no control characters.</summary>
public sealed record PersonName
{
    public const int MaxLength = 64;

    private PersonName(string value) => Value = value;

    public string Value { get; }

    /// <summary>Blank means "no value" and gives null. <paramref name="field"/> names the error code, e.g. <c>first_name</c>.</summary>
    public static Result<PersonName?> Create(string? raw, string field)
    {
        var value = Text.OrNull(raw);
        if (value is null)
        {
            return Result.Ok<PersonName?>(null);
        }

        if (value.Length > MaxLength || Text.HasControlCharacters(value))
        {
            return Result.Fail<PersonName?>(UserErrors.NameInvalid(field));
        }

        return new PersonName(value);
    }

    /// <summary>From Google or Apple through Auth: cleaned and cut, never fails.</summary>
    public static PersonName? FromRegistration(string? raw) =>
        Text.Sanitize(raw, MaxLength) is { } value ? new PersonName(value) : null;

    public override string ToString() => Value;
}
