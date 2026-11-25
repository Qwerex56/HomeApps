using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

internal sealed class CreateInviteHandler(
    IHouseholdRepository households, IInviteRepository invites, IUserCopies users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<CreateInviteCommand, InviteView>
{
    public async Task<Result<InviteView>> Handle(CreateInviteCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        if (household.IsFailed)
        {
            return household.ToResult<InviteView>();
        }

        var invite = household.Value.CreateInvite(currentUser.Actor(), command.Role, command.Validity, command.GuestAccessUntil, time);
        if (invite.IsFailed)
        {
            return invite.ToResult<InviteView>();
        }

        invites.Add(invite.Value);
        var me = await users.GetAsync(currentUser.RequiredUserId, cancellationToken);
        return invite.Value.ToView(me?.DisplayName);
    }
}
