namespace Auth.Application.ConnectedApps.RegisterApp;

public sealed record RegisteredAppView(string ClientId, string ClientName, IReadOnlyList<string> RedirectUris, DateTimeOffset IssuedAt);
