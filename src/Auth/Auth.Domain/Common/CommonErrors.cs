using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Auth.Domain.Common;

public static class CommonErrors
{
    public static Error EmailInvalid => new AppError("email.invalid", "The email address is not valid.", ErrorKind.Validation);

    public static Error PasswordTooWeak => new AppError("password.too_weak",
        $"The password must be {PasswordPolicy.MinLength}–{PasswordPolicy.MaxLength} characters long and different from the email.", ErrorKind.Validation);

    public static Error ClientInvalid => new AppError("client.unknown", "The client is not known.", ErrorKind.Validation);
}
