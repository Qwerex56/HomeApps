using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

internal sealed class TransferOwnershipHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<TransferOwnershipCommand>
{
    public async Task<Result> Handle(TransferOwnershipCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed
            ? household.ToResult()
            : household.Value.TransferOwnership(currentUser.Actor(), command.ToUserId, time);
    }
}
