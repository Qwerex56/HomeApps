using Auth.Application.Abstractions;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Delegation.DelegateToAssistant;

internal sealed partial class DelegateToAssistantHandler(
    IClientRegistry clients,
    IIdentityRepository identities,
    ITokenIssuer tokens,
    ILogger<DelegateToAssistantHandler> logger)
    : ICommandHandler<DelegateToAssistantCommand, DelegatedTokenView>
{
    public async Task<Result<DelegatedTokenView>> Handle(DelegateToAssistantCommand command, CancellationToken cancellationToken)
    {
        var service = clients.AuthenticateService(command.ClientId, command.ClientSecret);
        if (service is null)
        {
            LogRefusedClient(logger, command.ClientId);
            return Result.Fail<DelegatedTokenView>(DelegationErrors.ClientInvalid);
        }

        var identity = await identities.GetByIdAsync(command.UserId, cancellationToken);
        if (identity is null || identity.Status != AccountStatus.Active)
        {
            return Result.Fail<DelegatedTokenView>(DelegationErrors.UserUnavailable);
        }

        var token = tokens.IssueDelegatedToken(identity, service, service.ClientId);
        LogDelegated(logger, identity.Id, service.ClientId, command.Channel, command.Reference);
        return new DelegatedTokenView(token.Value, token.ExpiresAt);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Delegated a token for {UserId} to {ClientId} ({Channel} {Reference})")]
    private static partial void LogDelegated(ILogger logger, Guid userId, string clientId, string channel, string? reference);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refused a delegation for client {ClientId}: unknown client or wrong secret")]
    private static partial void LogRefusedClient(ILogger logger, string clientId);
}
