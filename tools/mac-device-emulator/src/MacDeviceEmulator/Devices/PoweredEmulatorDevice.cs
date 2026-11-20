using System.Net.WebSockets;
using System.Text.Json;
using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Configuration;
using MacDeviceEmulator.Protocol;
using MacDeviceEmulator.Services;

namespace MacDeviceEmulator.Devices;

public sealed class PoweredEmulatorDevice(
    string serverUrl,
    EmulatorStorage storage,
    string configPath,
    string? pairingCode,
    IHomeAppsClient client,
    Func<IDeviceWebSocket> webSocketFactory,
    IMacSystem macSystem,
    TimeProvider timeProvider) : IEmulatorDevice
{
    public string DeviceKey => "powered";
    public string Name => "Mac Powered (test)";

    private bool _simulatedLampOn = false;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{Name}] Starting device runner...");

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!storage.Devices.TryGetValue(DeviceKey, out var pairedState))
            {
                if (string.IsNullOrWhiteSpace(pairingCode))
                {
                    Console.WriteLine($"[{Name}] Not paired and no pairing code provided. Waiting...");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    continue;
                }

                Console.WriteLine($"[{Name}] Pairing with code '{pairingCode}'...");
                var pairResult = await client.PairAsync(serverUrl, pairingCode, Descriptors.PoweredDescriptor, cancellationToken);
                if (pairResult.IsFailed)
                {
                    Console.WriteLine($"[{Name}] Pairing failed: {pairResult.Errors.First().Message}. Retrying in 5s...");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    continue;
                }

                var pairData = pairResult.Value;
                pairedState = new PairedDeviceState
                {
                    DeviceId = pairData.DeviceId,
                    HouseholdId = pairData.HouseholdId,
                    Name = pairData.Name,
                    Secret = pairData.Secret,
                    Slug = pairData.Slug,
                    Purpose = pairData.Purpose
                };

                storage.Devices[DeviceKey] = pairedState;
                await storage.SaveAsync(configPath, cancellationToken);
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
                    storage.Devices.Remove(DeviceKey);
                    await storage.SaveAsync(configPath, cancellationToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                continue;
            }

            var token = tokenResult.Value;
            var wsUri = new Uri($"{serverUrl.TrimEnd('/').Replace("http://", "ws://").Replace("https://", "wss://")}/devices/link");

            await using var ws = webSocketFactory();
            try
            {
                Console.WriteLine($"[{Name}] Connecting WebSocket to {wsUri}...");
                await ws.ConnectAsync(wsUri, token, cancellationToken);

                using var descDoc = JsonDocument.Parse(Descriptors.PoweredDescriptor);
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

                await SendCurrentStateAsync(ws, cancellationToken);

                var lastPeriodicReport = timeProvider.GetUtcNow();

                while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var receiveTask = ws.ReceiveMessageAsync(cancellationToken);
                    var delayTask = Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    var completed = await Task.WhenAny(receiveTask, delayTask);
                    if (completed == receiveTask)
                    {
                        var msg = await receiveTask;
                        if (msg is null)
                        {
                            Console.WriteLine($"[{Name}] Link closed by server.");
                            break;
                        }

                        await HandleMessageAsync(ws, msg, cancellationToken);
                    }

                    if (timeProvider.GetUtcNow() - lastPeriodicReport >= TimeSpan.FromSeconds(60))
                    {
                        await SendCurrentStateAsync(ws, cancellationToken);

                        var statusFrame = new LinkMessage(Type: "status");
                        await ws.SendMessageAsync(statusFrame, cancellationToken);

                        lastPeriodicReport = timeProvider.GetUtcNow();
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"[{Name}] Connection error: {ex.Message}. Reconnecting in 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task SendCurrentStateAsync(IDeviceWebSocket ws, CancellationToken cancellationToken)
    {
        var vol = await macSystem.GetVolumeAsync(cancellationToken);
        var muted = await macSystem.IsMutedAsync(cancellationToken);
        var cpu = await macSystem.GetCpuLoadAsync(cancellationToken);

        var values = new Dictionary<string, object>
        {
            ["volume"] = vol,
            ["muted"] = muted,
            ["lamp.on"] = _simulatedLampOn,
            ["cpu_load"] = cpu
        };

        var doc = JsonDocument.Parse(JsonSerializer.Serialize(values));
        var rawMessage = new LinkMessage("state", Value: doc.RootElement);
        await ws.SendMessageAsync(rawMessage, cancellationToken);
    }

    private async Task HandleMessageAsync(IDeviceWebSocket ws, LinkMessage msg, CancellationToken cancellationToken)
    {
        if (msg.Type == "set" && msg.Id is { } setId && msg.Name is { } controlName)
        {
            Console.WriteLine($"[{Name}] Setting control '{controlName}' to {msg.Value}");
            if (controlName == "volume" && msg.Value is { } volElem && volElem.TryGetInt32(out var vol))
            {
                await macSystem.SetVolumeAsync(vol, cancellationToken);
                await ws.SendMessageAsync(new LinkMessage("set_result", Id: setId, Ok: true), cancellationToken);
                await SendCurrentStateAsync(ws, cancellationToken);
            }
            else if (controlName == "muted" && msg.Value is { } muteElem)
            {
                var isMuted = muteElem.GetBoolean();
                await macSystem.SetMutedAsync(isMuted, cancellationToken);
                await ws.SendMessageAsync(new LinkMessage("set_result", Id: setId, Ok: true), cancellationToken);
                await SendCurrentStateAsync(ws, cancellationToken);
            }
            else if (controlName == "lamp.on" && msg.Value is { } lampElem)
            {
                _simulatedLampOn = lampElem.GetBoolean();
                Console.WriteLine($"[{Name}] Simulated lamp turned {(_simulatedLampOn ? "ON" : "OFF")}");
                await ws.SendMessageAsync(new LinkMessage("set_result", Id: setId, Ok: true), cancellationToken);
                await SendCurrentStateAsync(ws, cancellationToken);
            }
            else
            {
                await ws.SendMessageAsync(new LinkMessage("set_result", Id: setId, Ok: false, Text: "Unknown control"), cancellationToken);
            }
        }
    }
}
