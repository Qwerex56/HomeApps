using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.ConnectedApps.RedeemCode;

/// <summary><c>grant_type=authorization_code</c>: the app exchanges the code with its PKCE verifier and gets its own session.</summary>
public sealed record RedeemCodeCommand(
    string? ClientId, string? Code, string? RedirectUri, string? CodeVerifier, string? Resource, string? UserAgent)
    : ICommand<AppTokens>;
