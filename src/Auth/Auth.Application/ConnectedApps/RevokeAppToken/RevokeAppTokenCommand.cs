using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.ConnectedApps.RevokeAppToken;

/// <summary>
/// RFC 7009: an app gives back its refresh token, which ends its session. Always succeeds, whatever the token: the
/// answer must not tell anything about it. Access tokens cannot be revoked; they live 15 minutes.
/// </summary>
public sealed record RevokeAppTokenCommand(string? Token, string? ClientId) : ICommand;
