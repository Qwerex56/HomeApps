using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.MyAccount;

/// <summary>A-10: my email, role and ways of signing in.</summary>
public sealed record GetMeQuery : IQuery<MeView>;

public sealed record MeView(Guid Id, string Email, ServerRole Role, bool HasPassword, IReadOnlyList<LinkedLoginView> Logins);

public sealed record LinkedLoginView(ExternalProvider Provider, string Email, DateTimeOffset LinkedAt);

internal sealed class GetMeHandler(IIdentityRepository identities, ICurrentUser currentUser) : IQueryHandler<GetMeQuery, MeView>
{
    public async Task<Result<MeView>> Handle(GetMeQuery query, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (identity is null)
        {
            return Result.Fail<MeView>(IdentityErrors.CannotSignIn);
        }

        return new MeView(
            identity.Id,
            identity.Email.Value,
            identity.Role,
            identity.HasPassword,
            [.. identity.ExternalLogins.Select(l => new LinkedLoginView(l.Provider, l.EmailAtLink.Value, l.LinkedAt))]);
    }
}
