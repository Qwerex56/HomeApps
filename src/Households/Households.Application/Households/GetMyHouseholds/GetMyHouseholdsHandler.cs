using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

internal sealed class GetMyHouseholdsHandler(IHouseholdDirectory directory, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<GetMyHouseholdsQuery, IReadOnlyList<MyHouseholdView>>
{
    public async Task<Result<IReadOnlyList<MyHouseholdView>>> Handle(GetMyHouseholdsQuery query, CancellationToken cancellationToken)
    {
        var items = await directory.MyHouseholdsAsync(currentUser.RequiredUserId, time.GetUtcNow(), cancellationToken);
        return items
            .Select(i => new MyHouseholdView(
                i.Id, i.Name, i.Description, i.MyRole,
                i.MyRole >= HouseholdRole.Member ? i.MemberCount : null,
                i.MyRole == HouseholdRole.Owner && i.OwnershipReceivedAt is not null))
            .ToList();
    }
}
