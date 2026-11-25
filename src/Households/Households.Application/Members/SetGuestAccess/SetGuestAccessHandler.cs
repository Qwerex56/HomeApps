using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

internal sealed class SetGuestAccessHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<SetGuestAccessCommand>
{
    public async Task<Result> Handle(SetGuestAccessCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed
            ? household.ToResult()
            : household.Value.SetGuestAccess(currentUser.Actor(), command.UserId, command.Until, time);
    }
}
