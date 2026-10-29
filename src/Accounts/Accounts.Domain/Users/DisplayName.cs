using Accounts.Domain.Common;
using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>The name shown everywhere (Household copies it): 2–64 characters, trimmed, no control characters.</summary>
public sealed record DisplayName
{
    public const int MinLength = 2;
    public const int MaxLength = 64;

    /// <summary>Used when a registration brings no usable name; the user changes it in the profile.</summary>
    public const string Fallback = "Użytkownik";

    private DisplayName(string value) => Value = value;

    public string Value { get; }

    public static Result<DisplayName> Create(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length is < MinLength or > MaxLength || Text.HasControlCharacters(value))
        {
            return Result.Fail<DisplayName>(UserErrors.DisplayNameInvalid);
        }

        return new DisplayName(value);
    }

    /// <summary>
    /// A name from Auth's <c>IdentityRegistered</c> must never fail the registration: Auth allows up to 100 characters,
    /// so the name is cleaned and cut to <see cref="MaxLength"/>; a name too short to use becomes <see cref="Fallback"/>.
    /// </summary>
    public static DisplayName FromRegistration(string? raw)
    {
        var value = Text.Sanitize(raw, MaxLength);
        return new DisplayName(value is { Length: >= MinLength } ? value : Fallback);
    }

    public override string ToString() => Value;
}
