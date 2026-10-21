using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Auth.Domain.Invitations;

public static class InvitationErrors
{
    public static Error InvalidOrExpired => new AppError("invitation.invalid_or_expired",
        "The invitation is not valid any more.", ErrorKind.Validation);

    public static Error AlreadyPending => new AppError("invitation.already_pending",
        "This email address already has a pending invitation.", ErrorKind.Conflict);

    public static Error CodeInvalid => new AppError("invitation.code_invalid", "This is not an invitation code.", ErrorKind.Validation);

    public static Error InviterCannotSignIn => new AppError("auth.cannot_sign_in",
        "Only an active account can invite people.", ErrorKind.Unauthorized);
}
