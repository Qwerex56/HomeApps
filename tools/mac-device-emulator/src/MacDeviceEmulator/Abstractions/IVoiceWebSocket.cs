using System.Net.WebSockets;
using MacDeviceEmulator.Protocol;

namespace MacDeviceEmulator.Abstractions;

/// <summary>WebSocket transport for the <c>/jarvis/voice?speech=local</c> channel.</summary>
public interface IVoiceWebSocket : IAsyncDisposable
{
    WebSocketState State { get; }

    Task ConnectAsync(Uri uri, string? bearerToken, CancellationToken cancellationToken = default);

    Task SendMessageAsync(VoiceMessage message, CancellationToken cancellationToken = default);

    Task<VoiceMessage?> ReceiveMessageAsync(CancellationToken cancellationToken = default);

    Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken = default);
}
