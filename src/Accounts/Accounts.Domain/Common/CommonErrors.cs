using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Accounts.Domain.Common;

public static class CommonErrors
{
    public static Error EmailInvalid => new AppError("email.invalid", "The email address is not valid.", ErrorKind.Validation);
}
