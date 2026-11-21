using System.Net.WebSockets;
using System.Text.Json;
using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Configuration;
using MacDeviceEmulator.Protocol;
using MacDeviceEmulator.Services;

namespace MacDeviceEmulator.Devices;

public sealed class SatelliteEmulatorDevice(
    string serverUrl,
    EmulatorStorage storage,
    string configPath,
    string? pairingCode,
    IHomeAppsClient client,
    Func<IDeviceWebSocket> webSocketFactory,
    IMacSystem macSystem,
    TimeProvider timeProvider) : IEmulatorDevice
{
    public string DeviceKey => "satellite";
    public string Name => "Mac Satellite (test)";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{Name}] Starting device runner...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!storage.TryGetDevice(DeviceKey, out var pairedState) || pairedState is null)
                {
                    if (string.IsNullOrWhiteSpace(pairingCode))
                    {
                        Console.WriteLine($"[{Name}] Not paired and no pairing code provided. Waiting...");
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                        continue;
                    }

                    Console.WriteLine($"[{Name}] Pairing with code '{pairingCode}'...");
                    var pairResult = await client.PairAsync(serverUrl, pairingCode, Descriptors.SatelliteDescriptor, cancellationToken);
                    if (pairResult.IsFailed)
                    {
                        Console.WriteLine($"[{Name}] Pairing failed: {pairResult.Errors.First().Message}. Retrying in 5s...");
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                        continue;
                    }

                    var pairData = pairResult.Value;
                    if (!pairData.Name.Contains("test", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[{Name}] WARNING: Paired device name '{pairData.Name}' does not contain '(test)'. Please name test devices with 'test' to avoid confusing production devices.");
                    }

                    pairedState = new PairedDeviceState
                    {
                        DeviceId = pairData.DeviceId,
                        HouseholdId = pairData.HouseholdId,
                        Name = pairData.Name,
                        Secret = pairData.Secret,
                        Slug = pairData.Slug,
                        Purpose = pairData.Purpose
                    };

                    await storage.SaveDeviceAsync(DeviceKey, pairedState, configPath, cancellationToken);
                    Console.WriteLine($"[{Name}] Paired successfully as {pairedState.Name} ({pairedState.DeviceId})");
                }

                Console.WriteLine($"[{Name}] Requesting device token...");
                var tokenResult = await client.GetDeviceTokenAsync(serverUrl, pairedState.DeviceId, pairedState.Secret, cancellationToken);
                if (tokenResult.IsFailed)
                {
                    var error = tokenResult.Errors.First().Message;
                    Console.WriteLine($"[{Name}] Token request failed: {error}");
                    if (error == "devices.device_unpaired")
                    {
                        Console.WriteLine($"[{Name}] Device was unpaired on server. Resetting stored credentials.");
                        await storage.RemoveDeviceAsync(DeviceKey, configPath, cancellationToken);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    continue;
                }

                var token = tokenResult.Value;
                var wsUri = new Uri($"{serverUrl.TrimEnd('/').Replace("http://", "ws://").Replace("https://", "wss://")}/devices/link");

                await using var ws = webSocketFactory();
                Console.WriteLine($"[{Name}] Connecting WebSocket to {wsUri}...");
                await ws.ConnectAsync(wsUri, token, cancellationToken);

                using var descDoc = JsonDocument.Parse(Descriptors.SatelliteDescriptor);
                var helloMessage = new LinkMessage(
                    Type: "hello",
                    Device: pairedState.Name,
                    Kind: "satellite",
                    Descriptor: descDoc.RootElement);

                await ws.SendMessageAsync(helloMessage, cancellationToken);

                var readyMsg = await ws.ReceiveMessageAsync(cancellationToken);
                if (readyMsg?.Type != "ready")
                {
                    Console.WriteLine($"[{Name}] Expected 'ready', got: {readyMsg?.Type} ({readyMsg?.Code})");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    continue;
                }

                Console.WriteLine($"[{Name}] Link ready! Connected as {readyMsg.DeviceId}");

                var initialVolume = await macSystem.GetVolumeAsync(cancellationToken);
                var initialMuted = await macSystem.IsMutedAsync(cancellationToken);
                await SendStateAsync(ws, initialVolume, initialMuted, cancellationToken);

                var lastHeartbeat = timeProvider.GetUtcNow();
                Task<LinkMessage?>? pendingReceiveTask = null;

                while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    pendingReceiveTask ??= ws.ReceiveMessageAsync(cancellationToken);
                    var delayTask = Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    var completed = await Task.WhenAny(pendingReceiveTask, delayTask);
                    if (completed == pendingReceiveTask)
                    {
                        var msg = await pendingReceiveTask;
                        pendingReceiveTask = null;

                        if (msg is null)
                        {
                            Console.WriteLine($"[{Name}] Link closed by server.");
                            break;
                        }

                        await HandleMessageAsync(ws, msg, cancellationToken);
                    }

                    if (timeProvider.GetUtcNow() - lastHeartbeat >= TimeSpan.FromSeconds(60))
                    {
                        var statusFrame = new LinkMessage(Type: "status");
                        await ws.SendMessageAsync(statusFrame, cancellationToken);
                        lastHeartbeat = timeProvider.GetUtcNow();
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"[{Name}] Error in runner loop: {ex.Message}. Reconnecting in 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private static async Task SendStateAsync(IDeviceWebSocket ws, int volume, bool muted, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object>
        {
            ["volume"] = volume,
            ["muted"] = muted
        };
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(values));
        var rawMessage = new LinkMessage("state", Values: doc.RootElement);
        await ws.SendMessageAsync(rawMessage, cancellationToken);
    }

    private async Task HandleMessageAsync(IDeviceWebSocket ws, LinkMessage msg, CancellationToken cancellationToken)
    {
        if (msg.Type == "action" && msg.Name == "say" && msg.Id is { } actionId)
        {
            var text = msg.Input?.TryGetProperty("text", out var textProp) == true ? textProp.GetString() : "Hello";
            Console.WriteLine($"[{Name}] Executing action 'say': \"{text}\"");
            if (!string.IsNullOrEmpty(text))
            {
                if (text.Length > 200)
                {
                    text = text[..200];
                }
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await macSystem.SayAsync(text, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{Name}] Speech failed: {ex.Message}");
                    }
                });
            }

            var answer = new LinkMessage("action_result", Id: actionId, Ok: true, Text: "Spoken");
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
