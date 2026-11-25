using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

internal sealed class PreviewInviteHandler(
    IHouseholdRepository households, IInviteRepository invites, IUserCopies users, TimeProvider time)
    : IQueryHandler<PreviewInviteQuery, InvitePreviewView>
{
    public async Task<Result<InvitePreviewView>> Handle(PreviewInviteQuery query, CancellationToken cancellationToken)
    {
        var target = await households.ResolveAsync(invites, query.Code, time.GetUtcNow(), cancellationToken);
        if (target.IsFailed)
        {
            return target.ToResult<InvitePreviewView>();
        }

        var (household, invite) = target.Value;
        if (invite is null)
        {
            return new InvitePreviewView(household.Id, household.Name.Value, HouseholdRole.Guest, null, null);
        }

        var author = invite.CreatedBy is { } by ? await users.GetAsync(by, cancellationToken) : null;
        return new InvitePreviewView(household.Id, household.Name.Value, invite.Role, author?.DisplayName, invite.ExpiresAt);
    }
}
