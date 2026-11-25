using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

internal sealed class RevokeInviteHandler(
    IHouseholdRepository households, IInviteRepository invites, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RevokeInviteCommand>
{
    public async Task<Result> Handle(RevokeInviteCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        if (household.IsFailed)
        {
            return household.ToResult();
        }

        var invite = await invites.GetByIdAsync(command.InviteId, cancellationToken);
        if (invite is null)
        {
            var role = household.Value.RoleOf(currentUser.Actor(), time.GetUtcNow());
            return role.IsFailed ? role.ToResult() : Result.Fail(InviteErrors.NotFound);
        }

        return household.Value.RevokeInvite(currentUser.Actor(), invite, time);
    }
}
