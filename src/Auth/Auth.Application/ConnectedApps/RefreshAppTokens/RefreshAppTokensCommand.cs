using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.ConnectedApps.RefreshAppTokens;

/// <summary><c>grant_type=refresh_token</c> for a connected app: rotation and reuse detection as for HomeApps' own sessions.</summary>
public sealed record RefreshAppTokensCommand(string? ClientId, string? RefreshToken, string? Resource, string? UserAgent)
    : ICommand<AppTokens>;
