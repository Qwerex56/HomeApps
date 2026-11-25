using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

internal sealed class AcceptInviteHandler(
    IHouseholdRepository households, IInviteRepository invites, IUserCopies users, ICurrentUser currentUser,
    HouseholdSettings settings, TimeProvider time)
    : ICommandHandler<AcceptInviteCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AcceptInviteCommand command, CancellationToken cancellationToken)
    {
        var target = await households.ResolveAsync(invites, command.Code, time.GetUtcNow(), cancellationToken);
        if (target.IsFailed)
        {
            return target.ToResult<Guid>();
        }

        var active = await users.EnsureActiveAsync(currentUser.RequiredUserId, cancellationToken);
        if (active.IsFailed)
        {
            return active.ToResult<Guid>();
        }

        var (household, invite) = target.Value;
        var joined = invite is null
            ? household.JoinAsGuest(currentUser.RequiredUserId, household.GuestCode!, settings.MaxMembers, time)
            : household.Accept(invite, currentUser.RequiredUserId, settings.MaxMembers, time);
        return joined.IsFailed ? joined.ToResult<Guid>() : household.Id;
    }
}
