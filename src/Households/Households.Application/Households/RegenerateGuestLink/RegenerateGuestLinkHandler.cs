using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

internal sealed class RegenerateGuestLinkHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RegenerateGuestLinkCommand, string>
{
    public async Task<Result<string>> Handle(RegenerateGuestLinkCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        if (household.IsFailed)
        {
            return household.ToResult<string>();
        }

        var code = household.Value.RegenerateGuestLink(currentUser.Actor(), time);
        return code.IsFailed ? code.ToResult<string>() : code.Value.Display;
    }
}
