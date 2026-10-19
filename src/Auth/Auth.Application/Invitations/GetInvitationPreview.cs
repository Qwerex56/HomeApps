using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Invitations;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Invitations;

/// <summary>A-07: what the invitation page shows before the guest decides: who invites and to which address.</summary>
public sealed record GetInvitationPreviewQuery(string Token) : IQuery<InvitationPreview>;

public sealed record InvitationPreview(string Email, string? InvitedByEmail, InvitationKind Kind, DateTimeOffset ExpiresAt);

internal sealed class GetInvitationPreviewValidator : AbstractValidator<GetInvitationPreviewQuery>
{
    public GetInvitationPreviewValidator() => RuleFor(q => q.Token).NotEmpty();
}

internal sealed class GetInvitationPreviewHandler(IInvitationRepository invitations, TimeProvider time)
    : IQueryHandler<GetInvitationPreviewQuery, InvitationPreview>
{
    public async Task<Result<InvitationPreview>> Handle(GetInvitationPreviewQuery query, CancellationToken cancellationToken)
    {
        var invitation = await invitations.GetByTokenHashAsync(OpaqueToken.HashOf(query.Token), cancellationToken);
        if (invitation?.Email is null || invitation.StatusAt(time.GetUtcNow()) != InvitationStatus.Pending)
        {
            return Result.Fail<InvitationPreview>(InvitationErrors.InvalidOrExpired);
        }

        return new InvitationPreview(
            invitation.Email.Value, invitation.InvitedByEmail?.Value, invitation.Kind, invitation.ExpiresAt);
    }
}
