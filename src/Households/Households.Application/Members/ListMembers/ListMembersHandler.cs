using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

internal sealed class ListMembersHandler(IHouseholdRepository households, IUserCopies users, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<ListMembersQuery, IReadOnlyList<MemberView>>
{
    public async Task<Result<IReadOnlyList<MemberView>>> Handle(ListMembersQuery query, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<IReadOnlyList<MemberView>>();
        }

        var household = found.Value;
        var actor = currentUser.Actor();
        var now = time.GetUtcNow();
        if (!household.CanSeeMembers(actor, now))
        {
            return Result.Fail<IReadOnlyList<MemberView>>(
                household.CanRead(actor, now) ? HouseholdErrors.Forbidden : HouseholdErrors.NotFound);
        }

        var ids = household.Members.Select(m => m.UserId)
            .Concat(household.Members.Where(m => m.InvitedBy is not null).Select(m => m.InvitedBy!.Value))
            .Distinct()
            .ToList();
        var copies = await users.GetManyAsync(ids, cancellationToken);
        return household.Members
            .Where(m => m.HasAccess(now))
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .Select(m =>
            {
                copies.TryGetValue(m.UserId, out var user);
                var displayName = user?.DisplayName ?? Domain.Users.UserCopy.FallbackName;
                string? invitedByName = m.InvitedBy is { } by && copies.TryGetValue(by, out var inviter) ? inviter.DisplayName : null;
                return new MemberView(
                    m.UserId, m.Nickname?.Value ?? displayName, m.Nickname?.Value, displayName, user?.AvatarUrl, m.Role,
                    user?.IsActive ?? true, m.JoinedAt, m.InvitedBy, invitedByName, m.AccessUntil);
            })
            .ToList();
    }
}
