using Notifications.Domain.Recipients;

namespace Notifications.Application.Abstractions;

/// <summary>Web Push (RFC 8030/8291/8292): encrypts the message for one browser and hands it to its push service.</summary>
public interface IPushSender
{
    Task<PushOutcome> SendAsync(PushTarget target, PushMessage message, CancellationToken cancellationToken);
}
