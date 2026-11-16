using HomeMix.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace HomeMix.Api.Realtime;

/// <summary>Sends the events to the groups of <see cref="HomeMixHub"/>; a group nobody is in costs nothing.</summary>
internal sealed class HubRealtimeSender(IHubContext<HomeMixHub> hub) : IRealtimeSender
{
    public async Task SendAsync(IReadOnlyList<RealtimeMessage> messages, CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
            await hub.Clients.Group(message.Group).SendAsync(HomeMixHub.ClientMethod, message.Event, message.Data, cancellationToken);
        }
    }
}
