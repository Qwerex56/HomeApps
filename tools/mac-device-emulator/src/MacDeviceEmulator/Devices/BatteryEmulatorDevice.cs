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
                    var pairResult = await client.PairAsync(serverUrl, pairingCode, Descriptors.BatteryDescriptor, cancellationToken);
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

                Console.WriteLine($"[{Name}] Requesting device token for /devices/report...");
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

                var batteryLevel = await macSystem.GetBatteryLevelAsync(cancellationToken);

                var frames = new List<DeviceFrame>();
                if (batteryLevel.HasValue)
                {
                    var stateValues = new Dictionary<string, object>
                    {
                        ["battery"] = batteryLevel.Value
                    };
                    using var stateDoc = JsonDocument.Parse(JsonSerializer.Serialize(stateValues));
                    frames.Add(new(Type: "state", Values: stateDoc.RootElement));
                    frames.Add(new(Type: "status", Battery: batteryLevel.Value));
                    Console.WriteLine($"[{Name}] Sending POST /devices/report (battery: {batteryLevel.Value}%)...");
                }
                else
                {
                    frames.Add(new(Type: "status"));
                    Console.WriteLine($"[{Name}] Sending POST /devices/report (status heartbeat, no battery detected)...");
                }

                var reportResult = await client.SendReportAsync(serverUrl, token, frames, cancellationToken);
                if (reportResult.IsFailed)
                {
                    var err = reportResult.Errors.First().Message;
                    Console.WriteLine($"[{Name}] Report failed: {err}");
                    if (err == "devices.device_unpaired")
                    {
                        await storage.RemoveDeviceAsync(DeviceKey, configPath, cancellationToken);
                    }
                }
                else
                {
                    Console.WriteLine($"[{Name}] Report accepted successfully ({reportResult.Value.Count} frames acknowledged).");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"[{Name}] Error in runner loop: {ex.Message}. Retrying in 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                continue;
            }

            Console.WriteLine($"[{Name}] Sleeping for 60 seconds (wake interval)...");
            await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
        }
    }
}
