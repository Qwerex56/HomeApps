using Auth.Domain.Common;
using FluentResults;

namespace Auth.Domain.Invitations;

public static class InvitationErrors
{
    public static Error InvalidOrExpired => AuthError.Create(
        "invitation.invalid_or_expired",
        "The invitation is not valid any more.");

    public static Error AlreadyPending => AuthError.Create(
        "invitation.already_pending",
        "This email address already has a pending invitation.");

    public static Error InviterCannotSignIn => AuthError.Create(
        "auth.cannot_sign_in",
        "Only an active account can invite people.");
}
