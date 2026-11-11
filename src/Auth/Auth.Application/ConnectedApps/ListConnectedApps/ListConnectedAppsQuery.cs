using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.ConnectedApps.ListConnectedApps;

/// <summary>"Connected apps" in the account settings. "Disconnect" is <c>DELETE /auth/me/sessions/{id}</c>.</summary>
public sealed record ListConnectedAppsQuery : IQuery<IReadOnlyList<ConnectedAppView>>;
