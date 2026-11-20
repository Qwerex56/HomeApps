using System.Text.Json;
using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Configuration;
using MacDeviceEmulator.Protocol;
using MacDeviceEmulator.Services;

namespace MacDeviceEmulator.Devices;

public sealed class BatteryEmulatorDevice(
    string serverUrl,
    EmulatorStorage storage,
    string configPath,
    string? pairingCode,
    IHomeAppsClient client,
    IMacSystem macSystem) : IEmulatorDevice
{
    public string DeviceKey => "battery";
    public string Name => "Mac Battery (test)";

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
                var pairResult = await client.PairAsync(serverUrl, pairingCode, Descriptors.BatteryDescriptor, cancellationToken);
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

            Console.WriteLine($"[{Name}] Requesting device token for /devices/report...");
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

            try
            {
                var batteryLevel = await macSystem.GetBatteryLevelAsync(cancellationToken) ?? 85;

                var stateValues = new Dictionary<string, object>
                {
                    ["battery"] = batteryLevel
                };
                var stateDoc = JsonDocument.Parse(JsonSerializer.Serialize(stateValues));

                var frames = new List<DeviceFrame>
                {
                    new(Type: "state", Values: stateDoc.RootElement),
                    new(Type: "status", Battery: batteryLevel)
                };

                Console.WriteLine($"[{Name}] Sending POST /devices/report (battery: {batteryLevel}%)...");
                var reportResult = await client.SendReportAsync(serverUrl, token, frames, cancellationToken);
                if (reportResult.IsFailed)
                {
                    var err = reportResult.Errors.First().Message;
                    Console.WriteLine($"[{Name}] Report failed: {err}");
                    if (err == "devices.device_unpaired")
                    {
                        storage.Devices.Remove(DeviceKey);
                        await storage.SaveAsync(configPath, cancellationToken);
                    }
                }
                else
                {
                    Console.WriteLine($"[{Name}] Report accepted successfully ({reportResult.Value.Count} frames acknowledged).");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"[{Name}] Error during report: {ex.Message}");
            }

            Console.WriteLine($"[{Name}] Sleeping for 60 seconds (wake interval)...");
            await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
        }
    }
}
