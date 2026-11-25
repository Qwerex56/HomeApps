using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

internal sealed class GetMyMembershipHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<GetMyMembershipQuery, MyMembershipView>
{
    public async Task<Result<MyMembershipView>> Handle(GetMyMembershipQuery query, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<MyMembershipView>();
        }

        var household = found.Value;
        if (currentUser.SatelliteHouseholdId is { } satelliteOf)
        {
            // S13: a satellite is no member; it speaks with the household assistant's voice, and only in its own household.
            var assistantRole = satelliteOf == household.Id ? household.AssistantRole() : Result.Fail<HouseholdRole>(HouseholdErrors.NotFound);
            return assistantRole.IsFailed
                ? assistantRole.ToResult<MyMembershipView>()
                : new MyMembershipView(household.Id, assistantRole.Value, household.Assistant.Name, household.CreatedAt, null, IsAssistant: true);
        }

        var role = household.RoleOf(currentUser.Actor(), time.GetUtcNow());
        if (role.IsFailed)
        {
            return role.ToResult<MyMembershipView>();
        }

        var me = household.MemberOf(currentUser.RequiredUserId)!;
        return new MyMembershipView(household.Id, me.Role, me.Nickname?.Value, me.JoinedAt, me.AccessUntil);
    }
}
