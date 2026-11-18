using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Households.Domain.Households;

public static class HouseholdErrors
{
    public static Error NameInvalid => new AppError("household.name_invalid",
        $"The household name must have 1–{HouseholdName.MaxLength} characters and no control characters.", ErrorKind.Validation);

    public static Error DescriptionInvalid => new AppError("household.description_invalid",
        $"The description may have at most {HouseholdDescription.MaxLength} characters and no control characters except new lines.",
        ErrorKind.Validation);

    public static Error NicknameInvalid => new AppError("household.nickname_invalid",
        $"The nickname may have at most {Nickname.MaxLength} characters and no control characters.", ErrorKind.Validation);

    public static Error RoleInvalid => new AppError("household.role_invalid",
        "Only Guest, Member and Admin can be given; ownership is handed over separately.", ErrorKind.Validation);

    public static Error AccessUntilInvalid => new AppError("household.access_until_invalid",
        "The end of a guest's access must be in the future.", ErrorKind.Validation);

    /// <summary>Q3: also for households the caller may not know about, and for archived ones (D7).</summary>
    public static Error NotFound => new AppError("household.not_found", "The household does not exist.", ErrorKind.NotFound);

    public static Error NotMember => new AppError("household.not_member", "This person is not a member of the household.", ErrorKind.NotFound);

    public static Error Forbidden => new AppError("household.forbidden", "Your role in this household does not allow this.", ErrorKind.Forbidden);

    public static Error AlreadyMember => new AppError("household.already_member", "You are already a member of this household.", ErrorKind.Conflict);

    public static Error Full => new AppError("household.full", "The household has reached its member limit.", ErrorKind.Conflict);

    public static Error OwnerCannotLeave => new AppError("household.owner_cannot_leave",
        "Hand the household over to another member before leaving it.", ErrorKind.Conflict);

    public static Error RoleUnchanged => new AppError("household.role_unchanged", "The member already has this role.", ErrorKind.Conflict);

    public static Error NotGuest => new AppError("household.not_guest", "Only a guest's access can end at a set time.", ErrorKind.Conflict);

    public static Error TransferTargetInvalid => new AppError("household.transfer_target_invalid",
        "Ownership can go only to a member or admin of the household, not to a guest or yourself.", ErrorKind.Conflict);

    public static Error AlreadyArchived => new AppError("household.archived", "The household is archived.", ErrorKind.Conflict);

    public static Error NotArchived => new AppError("household.not_archived", "The household is not archived.", ErrorKind.Conflict);

    public static Error NoOwner => new AppError("household.no_owner",
        "The household has no owner left; it cannot be restored.", ErrorKind.Conflict);
}
