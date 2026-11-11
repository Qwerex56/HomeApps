using Auth.Application.Abstractions;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.ConnectedApps.ListConnectedApps;

internal sealed class ListConnectedAppsHandler(
    IIdentityRepository identities, IAppClientRepository clients, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<ListConnectedAppsQuery, IReadOnlyList<ConnectedAppView>>
{
    public async Task<Result<IReadOnlyList<ConnectedAppView>>> Handle(ListConnectedAppsQuery query, CancellationToken cancellationToken)
    {
        var identity = await identities.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (identity is null)
        {
            return Result.Fail<IReadOnlyList<ConnectedAppView>>(IdentityErrors.CannotSignIn);
        }

        var sessions = identity.ConnectedApps(time.GetUtcNow());
        var ids = sessions.Select(s => s.Client.ClientId).Distinct(StringComparer.Ordinal).ToList();
        var names = (await clients.ListByIdsAsync(ids, cancellationToken)).ToDictionary(c => c.Id, c => c.Name, StringComparer.Ordinal);

        return sessions
            .Select(s => new ConnectedAppView(
                s.Id, s.Client.ClientId, names.GetValueOrDefault(s.Client.ClientId, s.Client.ClientId), s.StartedAt, s.LastUsedAt))
            .ToList();
    }
}
