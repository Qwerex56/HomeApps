using Households.Domain.Households;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Households.Domain.Invites;

public static class InviteErrors
{
    public static Error RoleInvalid => new AppError("invite.role_invalid",
        $"An invite gives the {HouseholdRole.Member} or {HouseholdRole.Guest} role; admins are promoted after joining.", ErrorKind.Validation);

    public static Error ValidityInvalid => new AppError("invite.validity_invalid",
        "The invite can be valid for 30 minutes, 1, 6 or 12 hours, 1 day or 7 days.", ErrorKind.Validation);

    /// <summary>Q3: a wrong or malformed code, or an invite of another household.</summary>
    public static Error NotFound => new AppError("invite.not_found", "There is no such invite.", ErrorKind.NotFound);

    public static Error NotUsable => new AppError("invite.not_usable", "The invite has expired or was revoked.", ErrorKind.Conflict);
}
