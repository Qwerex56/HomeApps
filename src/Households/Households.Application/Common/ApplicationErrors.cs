using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Households.Application.Common;

/// <summary>Errors that come from the use cases, not from a domain rule.</summary>
public static class ApplicationErrors
{
    public const string UserPendingCode = "user.pending";

    /// <summary>Q5: an event about an account whose <c>UserCreated</c> has not arrived yet; the consumer retries.</summary>
    public static Error UserPending => new AppError(UserPendingCode,
        "The account is not known here yet. Try again in a moment.", ErrorKind.NotFound);

    /// <summary>Q5: a disabled account creates no households and joins none.</summary>
    public static Error AccountDisabled => new AppError("account.disabled", "Your account is disabled.", ErrorKind.Forbidden);
}
