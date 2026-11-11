namespace Auth.Application.ConnectedApps.ListConnectedApps;

/// <param name="Id">The app's session; disconnecting ends it.</param>
public sealed record ConnectedAppView(Guid Id, string ClientId, string Name, DateTimeOffset ConnectedAt, DateTimeOffset LastUsedAt);
