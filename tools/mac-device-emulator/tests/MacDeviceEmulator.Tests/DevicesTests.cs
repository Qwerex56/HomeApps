using System.Net.WebSockets;
using System.Text.Json;
using FluentResults;
using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Configuration;
using MacDeviceEmulator.Devices;
using MacDeviceEmulator.Protocol;
using MacDeviceEmulator.Services;
using NSubstitute;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class DevicesTests
{
    [Fact]
    public async Task Satellite_pairs_connects_and_handles_say_action()
    {
        var storage = new EmulatorStorage();
        var client = Substitute.For<IHomeAppsClient>();
        var ws = Substitute.For<IDeviceWebSocket>();
        var mac = Substitute.For<IMacSystem>();

        var deviceId = Guid.NewGuid();
        var householdId = Guid.NewGuid();

        client.PairAsync(Arg.Any<string>(), "CODE123", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new PairResponse(deviceId, householdId, "Mac Satellite (test)", "sec1", "slug1", "assistant")));

        client.GetDeviceTokenAsync(Arg.Any<string>(), deviceId, "sec1", Arg.Any<CancellationToken>())
            .Returns(Result.Ok("test-token"));

        ws.State.Returns(WebSocketState.Open);

        using var sayDoc = JsonDocument.Parse("""{ "text": "Jarvis speaking" }""");

        var receiveSequence = new Queue<LinkMessage?>(new[]
        {
            new LinkMessage("ready", DeviceId: deviceId),
            new LinkMessage("action", Id: "act1", Name: "say", Input: sayDoc.RootElement),
            null
        });

        ws.ReceiveMessageAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(receiveSequence.Count > 0 ? receiveSequence.Dequeue() : null));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var tempConfig = Path.Combine(Path.GetTempPath(), $"sat-test-{Guid.NewGuid():N}.json");
        try
        {
            var satellite = new SatelliteEmulatorDevice(
                "http://localhost:5000",
                storage,
                tempConfig,
                "CODE123",
                client,
                () => ws,
                mac,
                TimeProvider.System);

            await satellite.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (File.Exists(tempConfig)) File.Delete(tempConfig);
        }

        await mac.Received(1).SayAsync("Jarvis speaking", Arg.Any<CancellationToken>());
        await ws.Received().SendMessageAsync(Arg.Is<LinkMessage>(m => m.Type == "action_result" && m.Id == "act1" && m.Ok == true), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Battery_device_sends_report_with_state_and_status()
    {
        var storage = new EmulatorStorage();
        var client = Substitute.For<IHomeAppsClient>();
        var mac = Substitute.For<IMacSystem>();

        var deviceId = Guid.NewGuid();
        var householdId = Guid.NewGuid();

        storage.Devices["battery"] = new PairedDeviceState
        {
            DeviceId = deviceId,
            HouseholdId = householdId,
            Name = "Mac Battery (test)",
            Secret = "sec-bat",
            Slug = "mac-battery-test",
            Purpose = "smart_home"
        };

        client.GetDeviceTokenAsync(Arg.Any<string>(), deviceId, "sec-bat", Arg.Any<CancellationToken>())
            .Returns(Result.Ok("bat-token"));

        mac.GetBatteryLevelAsync(Arg.Any<CancellationToken>()).Returns(91);

        client.SendReportAsync("http://localhost:5000", "bat-token", Arg.Any<IReadOnlyList<DeviceFrame>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok<IReadOnlyList<FrameResult>>(new List<FrameResult> { new(true), new(true) }));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var tempConfig = Path.Combine(Path.GetTempPath(), $"bat-test-{Guid.NewGuid():N}.json");

        try
        {
            var battery = new BatteryEmulatorDevice(
                "http://localhost:5000",
                storage,
                tempConfig,
                null,
                client,
                mac);

            await battery.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (File.Exists(tempConfig)) File.Delete(tempConfig);
        }

        await client.Received().SendReportAsync("http://localhost:5000", "bat-token", Arg.Is<IReadOnlyList<DeviceFrame>>(frames =>
            frames.Count == 2 &&
            frames.Any(f => f.Type == "state") &&
            frames.Any(f => f.Type == "status" && f.Battery == 91)
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Powered_device_handles_lamp_and_volume_set()
    {
        var storage = new EmulatorStorage();
        var client = Substitute.For<IHomeAppsClient>();
        var ws = Substitute.For<IDeviceWebSocket>();
        var mac = Substitute.For<IMacSystem>();

        var deviceId = Guid.NewGuid();
        var householdId = Guid.NewGuid();

        storage.Devices["powered"] = new PairedDeviceState
        {
            DeviceId = deviceId,
            HouseholdId = householdId,
            Name = "Mac Powered (test)",
            Secret = "sec-pow",
            Slug = "mac-powered-test",
            Purpose = "smart_home"
        };

        client.GetDeviceTokenAsync(Arg.Any<string>(), deviceId, "sec-pow", Arg.Any<CancellationToken>())
            .Returns(Result.Ok("pow-token"));

        ws.State.Returns(WebSocketState.Open);

        using var volDoc = JsonDocument.Parse("35");
        using var lampDoc = JsonDocument.Parse("true");

        var receiveSequence = new Queue<LinkMessage?>(new[]
        {
            new LinkMessage("ready", DeviceId: deviceId),
            new LinkMessage("set", Id: "set1", Name: "volume", Value: volDoc.RootElement),
            new LinkMessage("set", Id: "set2", Name: "lamp.on", Value: lampDoc.RootElement),
            null
        });

        ws.ReceiveMessageAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(receiveSequence.Count > 0 ? receiveSequence.Dequeue() : null));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var tempConfig = Path.Combine(Path.GetTempPath(), $"pow-test-{Guid.NewGuid():N}.json");

        try
        {
            var powered = new PoweredEmulatorDevice(
                "http://localhost:5000",
                storage,
                tempConfig,
                null,
                client,
                () => ws,
                mac,
                TimeProvider.System);

            await powered.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (File.Exists(tempConfig)) File.Delete(tempConfig);
        }

        await mac.Received(1).SetVolumeAsync(35, Arg.Any<CancellationToken>());
        await ws.Received().SendMessageAsync(Arg.Is<LinkMessage>(m => m.Type == "set_result" && m.Id == "set1" && m.Ok == true), Arg.Any<CancellationToken>());
        await ws.Received().SendMessageAsync(Arg.Is<LinkMessage>(m => m.Type == "set_result" && m.Id == "set2" && m.Ok == true), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Computer_device_connects_with_user_token_and_handles_mute_set()
    {
        var ws = Substitute.For<IDeviceWebSocket>();
        var mac = Substitute.For<IMacSystem>();

        ws.State.Returns(WebSocketState.Open);

        using var muteDoc = JsonDocument.Parse("true");

        var receiveSequence = new Queue<LinkMessage?>(new[]
        {
            new LinkMessage("ready", DeviceId: Guid.NewGuid()),
            new LinkMessage("set", Id: "set10", Name: "muted", Value: muteDoc.RootElement),
            null
        });

        ws.ReceiveMessageAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(receiveSequence.Count > 0 ? receiveSequence.Dequeue() : null));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var computer = new ComputerEmulatorDevice(
            "http://localhost:5000",
            "user-bearer-token-123",
            () => ws,
            mac);

        try
        {
            await computer.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        await ws.Received().ConnectAsync(Arg.Any<Uri>(), "user-bearer-token-123", Arg.Any<CancellationToken>());
        await mac.Received(1).SetMutedAsync(true, Arg.Any<CancellationToken>());
        await ws.Received().SendMessageAsync(Arg.Is<LinkMessage>(m => m.Type == "set_result" && m.Id == "set10" && m.Ok == true), Arg.Any<CancellationToken>());
    }
}
