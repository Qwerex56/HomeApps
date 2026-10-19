using FluentResults;

namespace Auth.Domain.Common;

public static class CommonErrors
{
    public static Error EmailInvalid => AuthError.Create("email.invalid", "The email address is not valid.");

    public static Error PasswordTooWeak => AuthError.Create(
        "password.too_weak",
        $"The password must be {PasswordPolicy.MinLength}–{PasswordPolicy.MaxLength} characters long and different from the email.");

    public static Error ClientInvalid => AuthError.Create("client.unknown", "The client is not known.");
}
