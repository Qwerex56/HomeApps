using Auth.Application.Abstractions;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.MyAccount;

/// <summary>A-14: my active sessions, most recently used first.</summary>
public sealed record ListSessionsQuery : IQuery<IReadOnlyList<SessionView>>;

public sealed record SessionView(
    Guid Id, string ClientId, string? UserAgent, AuthMethod Method, DateTimeOffset StartedAt, DateTimeOffset LastUsedAt);

internal sealed class ListSessionsHandler(IIdentityRepository identities, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<ListSessionsQuery, IReadOnlyList<SessionView>>
{
    public async Task<Result<IReadOnlyList<SessionView>>> Handle(ListSessionsQuery query, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (identity is null)
        {
            return Result.Fail<IReadOnlyList<SessionView>>(IdentityErrors.CannotSignIn);
        }

        return identity.ActiveSessions(time.GetUtcNow())
            .Select(s => new SessionView(s.Id, s.Client.ClientId, s.Client.UserAgent, s.Method, s.StartedAt, s.LastUsedAt))
            .ToList();
    }
}

/// <summary>A-14: ends one of my sessions (a lost phone). An unknown id changes nothing.</summary>
public sealed record RevokeSessionCommand(Guid SessionId) : ICommand;

internal sealed class RevokeSessionHandler(IIdentityRepository identities, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RevokeSessionCommand>
{
    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        identity?.SignOut(command.SessionId, time);
        return Result.Ok();
    }
}
