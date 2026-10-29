using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Auth.Domain.Identities;

public static class IdentityErrors
{
    public static Error EmailTaken => new AppError("email.taken", "This email address already has an account.", ErrorKind.Conflict);

    public static Error RegistrationClosed => new AppError("auth.registration_closed",
        "Registration is only possible with an invitation.", ErrorKind.Forbidden);

    public static Error ExternalLoginAlreadyLinked => new AppError("external_login.already_linked",
        "This external account is already linked to an identity.", ErrorKind.Conflict);

    public static Error ExternalSubjectInvalid => new AppError("external_login.invalid",
        "The external account identifier is not valid.", ErrorKind.Validation);

    public static Error DisplayNameRequired => new AppError("profile.display_name_required",
        $"The display name is required and may have at most {RegistrationProfile.DisplayNameMaxLength} characters.", ErrorKind.Validation);

    public static Error InvalidCredentials => new AppError("auth.invalid_credentials",
        "The email or password is not correct.", ErrorKind.Unauthorized);

    public static Error LockedOut => new AppError("auth.locked_out",
        "Too many failed attempts. Try again later.", ErrorKind.Locked);

    public static Error CannotSignIn => new AppError("auth.cannot_sign_in", "This account cannot sign in.", ErrorKind.Unauthorized);

    public static Error InvalidRefreshToken => new AppError("auth.invalid_refresh_token",
        "The session is not valid any more. Sign in again.", ErrorKind.Unauthorized);

    public static Error ReactivationPending => new AppError("reactivation.pending",
        "The account is being restored. Try again in a moment.", ErrorKind.Conflict);

    public static Error ReauthenticationRequired => new AppError("auth.reauthentication_required",
        "Sign in again before making this change.", ErrorKind.Unauthorized);

    public static Error TokenInvalidOrExpired => new AppError("token.invalid_or_expired",
        "The link is not valid any more.", ErrorKind.Validation);

    public static Error EmailUnchanged => new AppError("email.unchanged", "This is already the account's email address.", ErrorKind.Conflict);

    public static Error LastSignInMethod => new AppError("external_login.last_method",
        "The last way of signing in cannot be removed.", ErrorKind.Conflict);

    public static Error ForbiddenRole => new AppError("forbidden.role",
        "Your role does not allow this.", ErrorKind.Forbidden);
}
