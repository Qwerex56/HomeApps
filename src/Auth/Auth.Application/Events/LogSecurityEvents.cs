using Auth.Domain.Identities.Events;
using HomeApps.BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Logging;

namespace Auth.Application.Events;

/// <summary>R7: a reused or foreign refresh token is worth a warning in the log. Never logs the token itself.</summary>
internal sealed partial class LogSecurityEvents(ILogger<LogSecurityEvents> logger) : IDomainEventHandler<RefreshTokenReuseDetected>
{
    public Task Handle(DomainEventNotification<RefreshTokenReuseDetected> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var e = notification.DomainEvent;
        LogReuse(logger, e.IdentityId, e.SessionId, e.ClientId);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Refresh token reuse for identity {IdentityId}: session {SessionId} revoked (presented by client {ClientId})")]
    private static partial void LogReuse(ILogger logger, Guid identityId, Guid sessionId, string clientId);
}
