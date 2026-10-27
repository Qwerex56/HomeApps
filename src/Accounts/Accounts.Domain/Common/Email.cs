using System.Text.RegularExpressions;
using FluentResults;

namespace Accounts.Domain.Common;

/// <summary>Read-only copy of the sign-in email from Auth (D2), normalised the same way: trimmed and lower-case.</summary>
public sealed partial record Email
{
    public const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? raw)
    {
        var value = raw?.Trim().ToLowerInvariant() ?? string.Empty;
        if (value.Length is 0 or > MaxLength || !Pattern().IsMatch(value))
        {
            return Result.Fail<Email>(CommonErrors.EmailInvalid);
        }

        return new Email(value);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
