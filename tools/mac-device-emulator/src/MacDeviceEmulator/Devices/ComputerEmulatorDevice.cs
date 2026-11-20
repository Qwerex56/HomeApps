using System.Net.WebSockets;
using System.Text.Json;
using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Protocol;
using MacDeviceEmulator.Services;

namespace MacDeviceEmulator.Devices;

public sealed class ComputerEmulatorDevice(
    string serverUrl,
    string? userToken,
    Func<IDeviceWebSocket> webSocketFactory,
    IMacSystem macSystem) : IEmulatorDevice
{
    public string DeviceKey => "computer";
    public string Name => "Mac Computer (test)";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{Name}] Starting device runner...");

        while (!cancellationToken.IsCancellationRequested)
        {
            if (string.IsNullOrWhiteSpace(userToken))
            {
                Console.WriteLine($"[{Name}] No user Bearer token provided for computer device. Waiting...");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                continue;
            }

            var wsUri = new Uri($"{serverUrl.TrimEnd('/').Replace("http://", "ws://").Replace("https://", "wss://")}/devices/link");

            await using var ws = webSocketFactory();
            try
            {
                Console.WriteLine($"[{Name}] Connecting WebSocket to {wsUri} with user Bearer token...");
                await ws.ConnectAsync(wsUri, userToken, cancellationToken);

                using var descDoc = JsonDocument.Parse(Descriptors.ComputerDescriptor);
                var helloMessage = new LinkMessage(
                    Type: "hello",
                    Device: "MacBook (test)",
                    Kind: "computer",
                    Descriptor: descDoc.RootElement);

                await ws.SendMessageAsync(helloMessage, cancellationToken);

                var readyMsg = await ws.ReceiveMessageAsync(cancellationToken);
                if (readyMsg?.Type != "ready")
                {
                    Console.WriteLine($"[{Name}] Expected 'ready', got: {readyMsg?.Type} ({readyMsg?.Code})");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    continue;
                }

                Console.WriteLine($"[{Name}] Computer link ready! Connected as {readyMsg.DeviceId}");

                var initialVolume = await macSystem.GetVolumeAsync(cancellationToken);
                var initialMuted = await macSystem.IsMutedAsync(cancellationToken);
                await SendStateAsync(ws, initialVolume, initialMuted, cancellationToken);

                while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var msg = await ws.ReceiveMessageAsync(cancellationToken);
                    if (msg is null)
                    {
                        Console.WriteLine($"[{Name}] Link closed by server.");
                        break;
                    }

                    await HandleMessageAsync(ws, msg, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"[{Name}] Connection error: {ex.Message}. Reconnecting in 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task SendStateAsync(IDeviceWebSocket ws, int volume, bool muted, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object>
        {
            ["volume"] = volume,
            ["muted"] = muted
        };
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(values));
        var rawMessage = new LinkMessage("state", Value: doc.RootElement);
        await ws.SendMessageAsync(rawMessage, cancellationToken);
    }

    private async Task HandleMessageAsync(IDeviceWebSocket ws, LinkMessage msg, CancellationToken cancellationToken)
    {
        if (msg.Type == "action" && msg.Name == "say" && msg.Id is { } actionId)
        {
            var text = msg.Input?.TryGetProperty("text", out var textProp) == true ? textProp.GetString() : "Hello from Computer";
            Console.WriteLine($"[{Name}] Executing action 'say': \"{text}\"");
            if (!string.IsNullOrEmpty(text))
            {
                await macSystem.SayAsync(text, cancellationToken);
            }

            var answer = new LinkMessage("action_result", Id: actionId, Ok: true, Text: "Spoken by computer");
            await ws.SendMessageAsync(answer, cancellationToken);
        }
        else if (msg.Type == "set" && msg.Id is { } setId && msg.Name is { } controlName)
        {
            Console.WriteLine($"[{Name}] Setting control '{controlName}' to {msg.Value}");
            if (controlName == "volume" && msg.Value is { } volElem && volElem.TryGetInt32(out var vol))
            {
                await macSystem.SetVolumeAsync(vol, cancellationToken);
                await ws.SendMessageAsync(new LinkMessage("set_result", Id: setId, Ok: true), cancellationToken);
                var muted = await macSystem.IsMutedAsync(cancellationToken);
                await SendStateAsync(ws, vol, muted, cancellationToken);
            }
            else if (controlName == "muted" && msg.Value is { } muteElem)
            {
                var isMuted = muteElem.GetBoolean();
                await macSystem.SetMutedAsync(isMuted, cancellationToken);
                await ws.SendMessageAsync(new LinkMessage("set_result", Id: setId, Ok: true), cancellationToken);
                var currentVol = await macSystem.GetVolumeAsync(cancellationToken);
                await SendStateAsync(ws, currentVol, isMuted, cancellationToken);
            }
            else
            {
                await ws.SendMessageAsync(new LinkMessage("set_result", Id: setId, Ok: false, Text: "Unknown control"), cancellationToken);
            }
        }
    }
}
