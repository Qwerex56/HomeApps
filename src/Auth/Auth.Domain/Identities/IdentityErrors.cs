using Auth.Domain.Common;
using FluentResults;

namespace Auth.Domain.Identities;

public static class IdentityErrors
{
    public static Error EmailTaken => AuthError.Create("email.taken", "This email address already has an account.");

    public static Error RegistrationClosed => AuthError.Create(
        "auth.registration_closed",
        "Registration is only possible with an invitation.");

    public static Error ExternalLoginAlreadyLinked => AuthError.Create(
        "external_login.already_linked",
        "This external account is already linked to an identity.");

    public static Error ExternalSubjectInvalid => AuthError.Create(
        "external_login.invalid",
        "The external account identifier is not valid.");

    public static Error DisplayNameRequired => AuthError.Create(
        "profile.display_name_required",
        $"The display name is required and may have at most {RegistrationProfile.DisplayNameMaxLength} characters.");

    public static Error InvalidCredentials => AuthError.Create(
        "auth.invalid_credentials",
        "The email or password is not correct.");

    public static Error LockedOut => AuthError.Create(
        "auth.locked_out",
        "Too many failed attempts. Try again later.");

    public static Error CannotSignIn => AuthError.Create("auth.cannot_sign_in", "This account cannot sign in.");

    public static Error InvalidRefreshToken => AuthError.Create(
        "auth.invalid_refresh_token",
        "The session is not valid any more. Sign in again.");

    public static Error ReactivationPending => AuthError.Create(
        "reactivation.pending",
        "The account is being restored. Try again in a moment.");

    public static Error ReauthenticationRequired => AuthError.Create(
        "auth.reauthentication_required",
        "Sign in again before making this change.");

    public static Error TokenInvalidOrExpired => AuthError.Create(
        "token.invalid_or_expired",
        "The link is not valid any more.");

    public static Error EmailUnchanged => AuthError.Create("email.unchanged", "This is already the account's email address.");

    public static Error LastSignInMethod => AuthError.Create(
        "external_login.last_method",
        "The last way of signing in cannot be removed.");

    public static Error ForbiddenRole => AuthError.Create(
        "forbidden.role",
        "Your role does not allow this.");
}
