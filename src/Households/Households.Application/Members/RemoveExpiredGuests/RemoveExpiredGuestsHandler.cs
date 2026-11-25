using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

internal sealed class RemoveExpiredGuestsHandler(IHouseholdRepository households, TimeProvider time)
    : ICommandHandler<RemoveExpiredGuestsCommand, int>
{
    public async Task<Result<int>> Handle(RemoveExpiredGuestsCommand command, CancellationToken cancellationToken)
    {
        var expired = await households.ListWithExpiredGuestsAsync(time.GetUtcNow(), cancellationToken);
        return expired.Sum(h => h.RemoveExpiredGuests(time));
    }
}
