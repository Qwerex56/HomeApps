using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Auth.Application.Common;

/// <summary>Errors that come from the use cases, not from a domain rule.</summary>
public static class ApplicationErrors
{
    public static Error TicketInvalid => new AppError("ticket.invalid_or_expired",
        "The ticket is not valid any more. Sign in again.", ErrorKind.Unauthorized);

    public static Error IdentityNotFound => new AppError("identity.not_found", "There is no such account.", ErrorKind.NotFound);

    public static Error InvitationNotFound => new AppError("invitation.not_found", "There is no such invitation.", ErrorKind.NotFound);

    public static Error ExternalEmailNotVerified => new AppError("external_login.email_not_verified",
        "The provider has not verified this email address.", ErrorKind.Forbidden);
}
