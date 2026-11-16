namespace HomeMix.Application.Realtime;

/// <summary>Sends events to the hub's groups (the SignalR hub in the Api). Never throws for an empty group.</summary>
public interface IRealtimeSender
{
    Task SendAsync(IReadOnlyList<RealtimeMessage> messages, CancellationToken cancellationToken);
}
