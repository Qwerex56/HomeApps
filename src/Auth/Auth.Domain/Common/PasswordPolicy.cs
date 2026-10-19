using FluentResults;

namespace Auth.Domain.Common;

/// <summary>R2: 12–128 characters and different from the email. No composition rules (NIST 800-63B).</summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    public static Result Validate(string? password, Email email)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (password is null
            || password.Length is < MinLength or > MaxLength
            || string.Equals(password, email.Value, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail(CommonErrors.PasswordTooWeak);
        }

        return Result.Ok();
    }
}
