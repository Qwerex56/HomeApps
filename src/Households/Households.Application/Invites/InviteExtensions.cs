using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

internal static class InviteExtensions
{
    public static InviteView ToView(this Invite invite, string? createdByName) =>
        new(invite.Id, invite.Code.Display, invite.Role, invite.CreatedAt, invite.ExpiresAt, invite.GuestAccessUntil, invite.Uses,
            invite.CreatedBy, createdByName);

    /// <summary>
    /// The active household a code leads to, with the invite when it is an invite code and null for the guest link.
    /// Anything else is <c>invite.not_found</c>.
    /// </summary>
    public static async Task<Result<(Household Household, Invite? Invite)>> ResolveAsync(
        this IHouseholdRepository households, IInviteRepository invites, string code, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var parsed = InviteCode.Parse(code);
        if (parsed.IsFailed)
        {
            return parsed.ToResult<(Household, Invite?)>();
        }

        var invite = await invites.GetByCodeAsync(parsed.Value, cancellationToken);
        var household = invite is not null
            ? await households.GetByIdAsync(invite.HouseholdId, cancellationToken)
            : await households.GetByGuestCodeAsync(parsed.Value, cancellationToken);
        if (household is not { Status: HouseholdStatus.Active } || (invite is not null && !invite.IsUsable(now)))
        {
            return Result.Fail<(Household, Invite?)>(InviteErrors.NotFound);
        }

        return (household, invite);
    }
}
