using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Users;

namespace Households.Application.Members.ListMemberNames;

internal sealed class ListMemberNamesHandler(IHouseholdRepository households, IUserCopies users, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<ListMemberNamesQuery, IReadOnlyList<MemberNameView>>
{
    public async Task<Result<IReadOnlyList<MemberNameView>>> Handle(ListMemberNamesQuery query, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<IReadOnlyList<MemberNameView>>();
        }

        var household = found.Value;
        var now = time.GetUtcNow();
        if (!household.CanRead(currentUser.Actor(), now))
        {
            return Result.Fail<IReadOnlyList<MemberNameView>>(HouseholdErrors.NotFound);
        }

        var members = household.Members.Where(m => m.HasAccess(now)).ToList();
        var copies = await users.GetManyAsync(members.Select(m => m.UserId).ToList(), cancellationToken);
        return members
            .Select(m =>
            {
                copies.TryGetValue(m.UserId, out var user);
                return new MemberNameView(m.UserId, m.Nickname?.Value ?? user?.DisplayName ?? UserCopy.FallbackName, user?.AvatarUrl);
            })
            .OrderBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
