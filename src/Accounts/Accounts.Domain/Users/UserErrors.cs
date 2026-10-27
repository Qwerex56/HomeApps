using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Accounts.Domain.Users;

public static class UserErrors
{
    public static Error DisplayNameInvalid => new AppError("profile.display_name_invalid",
        $"The display name must have {DisplayName.MinLength}–{DisplayName.MaxLength} characters and no control characters.",
        ErrorKind.Validation);

    public static Error NameInvalid(string field) => new AppError($"profile.{field}_invalid",
        $"The {field.Replace('_', ' ')} may have at most {PersonName.MaxLength} characters and no control characters.",
        ErrorKind.Validation);

    public static Error LocaleInvalid => new AppError("profile.locale_invalid",
        "The language must be a tag like pl-PL or en.", ErrorKind.Validation);

    public static Error TimeZoneInvalid => new AppError("profile.time_zone_invalid",
        "The time zone must be an IANA zone like Europe/Warsaw.", ErrorKind.Validation);

    public static Error AvatarUrlInvalid => new AppError("profile.avatar_url_invalid",
        $"The avatar must be an https address of at most {AvatarUrl.MaxLength} characters.", ErrorKind.Validation);

    public static Error DateOfBirthInvalid => new AppError("profile.date_of_birth_invalid",
        "The date of birth cannot be in the future or before 1900.", ErrorKind.Validation);

    public static Error ForbiddenRole => new AppError("forbidden.role", "Your role does not allow this.", ErrorKind.Forbidden);

    public static Error OwnerProtected => new AppError("account.owner_protected",
        "The Owner's account cannot be disabled or erased.", ErrorKind.Forbidden);

    public static Error AccountDisabled => new AppError("account.disabled",
        "A disabled account is changed only by an administrator, or restored by signing in.", ErrorKind.Forbidden);

    public static Error AlreadyDisabled => new AppError("account.already_disabled", "The account is already disabled.", ErrorKind.Conflict);

    public static Error NotDisabled => new AppError("account.not_disabled", "The account is not disabled.", ErrorKind.Conflict);

    public static Error ReactivationNotAllowed => new AppError("reactivation.not_allowed",
        "An administrator disabled this account; only an administrator can enable it.", ErrorKind.Forbidden);

    public static Error EraseNotDisabled => new AppError("erase.not_disabled",
        "Only a disabled account can be erased. Disable it first.", ErrorKind.Conflict);

    public static Error EraseConfirmationMismatch => new AppError("erase.confirmation_mismatch",
        "The confirmation does not match the account's email address.", ErrorKind.Validation);
}
