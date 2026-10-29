using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Accounts.Application.Common;

/// <summary>Errors that come from the use cases, not from a domain rule.</summary>
public static class ApplicationErrors
{
    /// <summary>UC-01: the identity exists in Auth but its <c>IdentityRegistered</c> has not been applied yet; retry shortly.</summary>
    public static Error UserPending => new AppError("user.pending",
        "The account is still being created. Try again in a moment.", ErrorKind.NotFound);

    public const string UserPendingCode = "user.pending";

    public static Error UserNotFound => new AppError("user.not_found", "There is no such account.", ErrorKind.NotFound);
}
