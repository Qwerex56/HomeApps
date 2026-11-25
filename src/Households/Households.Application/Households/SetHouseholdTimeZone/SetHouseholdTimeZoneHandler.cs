using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Households.Application.Abstractions;
using Households.Application.Common;

namespace Households.Application.Households;

internal sealed class SetHouseholdTimeZoneHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<SetHouseholdTimeZoneCommand>
{
    public async Task<Result> Handle(SetHouseholdTimeZoneCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed
            ? household.ToResult()
            : household.Value.SetTimeZone(currentUser.Actor(), command.TimeZone, time);
    }
}
