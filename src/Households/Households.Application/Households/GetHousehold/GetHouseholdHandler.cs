using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

internal sealed class GetHouseholdHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<GetHouseholdQuery, HouseholdView>
{
    public async Task<Result<HouseholdView>> Handle(GetHouseholdQuery query, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<HouseholdView>();
        }

        var household = found.Value;
        var actor = currentUser.Actor();
        var now = time.GetUtcNow();
        if (!household.CanRead(actor, now))
        {
            return Result.Fail<HouseholdView>(HouseholdErrors.NotFound);
        }

        var role = household.RoleOf(actor, now);
        HouseholdRole? myRole = role.IsSuccess ? role.Value : null;
        return new HouseholdView(
            household.Id,
            household.Name.Value,
            household.Description?.Value,
            household.Status,
            household.CreatedAt,
            household.ArchivedAt,
            myRole,
            household.CanSeeMembers(actor, now) ? household.Members.Count : null,
            myRole >= HouseholdRole.Admin ? household.GuestCode?.Display : null,
            myRole == HouseholdRole.Owner ? household.OwnershipReceivedAt : null);
    }
}
