using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Invitations;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Invitations;

/// <summary>A-21: the caller's own invitations; with <paramref name="All"/> everyone's, for Admin and Owner (R10).</summary>
public sealed record ListInvitationsQuery(bool All, InvitationStatus? Status) : IQuery<IReadOnlyList<InvitationView>>;

public sealed record InvitationView(
    Guid Id,
    string? Email,
    string Code,
    InvitationKind Kind,
    InvitationStatus Status,
    string? InvitedByEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? AcceptedAt);

internal sealed class ListInvitationsHandler(IInvitationRepository invitations, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<ListInvitationsQuery, IReadOnlyList<InvitationView>>
{
    public async Task<Result<IReadOnlyList<InvitationView>>> Handle(ListInvitationsQuery query, CancellationToken cancellationToken)
    {
        if (query.All && currentUser.ServerRole() < ServerRole.Admin)
        {
            return Result.Fail<IReadOnlyList<InvitationView>>(IdentityErrors.ForbiddenRole);
        }

        var now = time.GetUtcNow();
        var list = await invitations.ListAsync(query.All ? null : currentUser.RequiredUserId, cancellationToken);
        return list
            .Select(i => new InvitationView(
                i.Id, i.Email?.Value, i.Code.Value, i.Kind, i.StatusAt(now), i.InvitedByEmail?.Value,
                i.CreatedAt, i.ExpiresAt, i.AcceptedAt))
            .Where(v => query.Status is null || v.Status == query.Status)
            .ToList();
    }
}
