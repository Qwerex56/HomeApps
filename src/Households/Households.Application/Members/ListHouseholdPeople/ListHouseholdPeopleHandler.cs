using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Users;

namespace Households.Application.Members.ListHouseholdPeople;

internal sealed class ListHouseholdPeopleHandler(IHouseholdRepository households, IUserCopies users, TimeProvider time)
    : IQueryHandler<ListHouseholdPeopleQuery, IReadOnlyList<MemberView>>
{
    public async Task<Result<IReadOnlyList<MemberView>>> Handle(ListHouseholdPeopleQuery query, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<IReadOnlyList<MemberView>>();
        }

        if (found.Value.Status == HouseholdStatus.Archived)
        {
            return Result.Fail<IReadOnlyList<MemberView>>(HouseholdErrors.NotFound);
        }

        var now = time.GetUtcNow();
        var members = found.Value.Members.Where(m => m.HasAccess(now)).OrderByDescending(m => m.Role).ThenBy(m => m.JoinedAt).ToList();
        var copies = await users.GetManyAsync(members.Select(m => m.UserId).ToList(), cancellationToken);
        return members
            .Select(m =>
            {
                copies.TryGetValue(m.UserId, out var user);
                var displayName = user?.DisplayName ?? UserCopy.FallbackName;
                return new MemberView(
                    m.UserId, m.Nickname?.Value ?? displayName, m.Nickname?.Value, displayName, user?.AvatarUrl, m.Role,
                    user?.IsActive ?? true, m.JoinedAt, null, null, m.AccessUntil);
            })
            .ToList();
    }
}
