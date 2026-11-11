using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.ConnectedApps.RegisterApp;

/// <summary>
/// RFC 7591: an app registers itself (Claude Code and claude.ai do it on their own). Anyone may call it, so it only
/// stores a name and redirect URIs; nothing works until a signed-in person allows the app on the consent screen.
/// </summary>
public sealed record RegisterAppCommand(
    string? ClientName,
    IReadOnlyList<string>? RedirectUris,
    IReadOnlyList<string>? GrantTypes,
    IReadOnlyList<string>? ResponseTypes,
    string? TokenEndpointAuthMethod) : ICommand<RegisteredAppView>;
