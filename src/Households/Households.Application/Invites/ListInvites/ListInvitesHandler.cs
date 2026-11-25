using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

internal sealed class ListInvitesHandler(
    IHouseholdRepository households, IInviteRepository invites, IUserCopies users, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<ListInvitesQuery, IReadOnlyList<InviteView>>
{
    public async Task<Result<IReadOnlyList<InviteView>>> Handle(ListInvitesQuery query, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (household.IsFailed)
        {
            return household.ToResult<IReadOnlyList<InviteView>>();
        }

        var visible = household.Value.InvitesVisibleTo(currentUser.Actor(), time);
        if (visible.IsFailed)
        {
            return visible.ToResult<IReadOnlyList<InviteView>>();
        }

        var now = time.GetUtcNow();
        var list = (await invites.ListNotRevokedAsync(household.Value.Id, visible.Value, cancellationToken))
            .Where(i => i.IsUsable(now))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
        var authors = await users.GetManyAsync(
            list.Where(i => i.CreatedBy is not null).Select(i => i.CreatedBy!.Value).Distinct().ToList(), cancellationToken);
        return list
            .Select(i => i.ToView(i.CreatedBy is { } by && authors.TryGetValue(by, out var author) ? author.DisplayName : null))
            .ToList();
    }
}
