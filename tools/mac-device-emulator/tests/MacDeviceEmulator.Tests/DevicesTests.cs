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
    public async Task Satellite_sends_state_using_values_property_not_value()
    {
        var storage = new EmulatorStorage();
        var client = Substitute.For<IHomeAppsClient>();
        var ws = Substitute.For<IDeviceWebSocket>();
        var mac = Substitute.For<IMacSystem>();

        var deviceId = Guid.NewGuid();
        storage.Devices["satellite"] = new PairedDeviceState
        {
            DeviceId = deviceId,
            HouseholdId = Guid.NewGuid(),
            Name = "Mac Satellite (test)",
            Secret = "sec1",
            Slug = "mac-satellite-test",
            Purpose = "assistant"
        };

        client.GetDeviceTokenAsync(Arg.Any<string>(), deviceId, "sec1", Arg.Any<CancellationToken>())
            .Returns(Result.Ok("token1"));

        ws.State.Returns(WebSocketState.Open);
        mac.GetVolumeAsync(Arg.Any<CancellationToken>()).Returns(40);
        mac.IsMutedAsync(Arg.Any<CancellationToken>()).Returns(false);

        var receiveSequence = new Queue<LinkMessage?>(new[]
        {
            new LinkMessage("ready", DeviceId: deviceId),
            null
        });

        ws.ReceiveMessageAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(receiveSequence.Count > 0 ? receiveSequence.Dequeue() : null));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var tempConfig = Path.Combine(Path.GetTempPath(), $"sat-val-{Guid.NewGuid():N}.json");

        try
        {
            var satellite = new SatelliteEmulatorDevice(
                "http://localhost:5000",
                storage,
                tempConfig,
                null,
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

        await ws.Received().SendMessageAsync(Arg.Is<LinkMessage>(m => m.Type == "state" && m.Values != null && m.Value == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Satellite_does_not_issue_concurrent_receive_calls_while_waiting()
    {
        var storage = new EmulatorStorage();
        var client = Substitute.For<IHomeAppsClient>();
        var ws = Substitute.For<IDeviceWebSocket>();
        var mac = Substitute.For<IMacSystem>();

        var deviceId = Guid.NewGuid();
        storage.Devices["satellite"] = new PairedDeviceState
        {
            DeviceId = deviceId,
            HouseholdId = Guid.NewGuid(),
            Name = "Mac Satellite (test)",
            Secret = "sec1",
            Slug = "mac-satellite-test",
            Purpose = "assistant"
        };

        client.GetDeviceTokenAsync(Arg.Any<string>(), deviceId, "sec1", Arg.Any<CancellationToken>())
            .Returns(Result.Ok("token1"));

        ws.State.Returns(WebSocketState.Open);

        var receiveCalls = 0;
        var inFlightReceives = 0;
        var maxConcurrentReceives = 0;
        var tcs = new TaskCompletionSource<LinkMessage?>();

        ws.ReceiveMessageAsync(Arg.Any<CancellationToken>()).Returns(async callInfo =>
        {
            var callIndex = Interlocked.Increment(ref receiveCalls);
            // First call is waiting for "ready" message
            if (callIndex == 1)
            {
                return new LinkMessage("ready", DeviceId: deviceId);
            }

            var current = Interlocked.Increment(ref inFlightReceives);
            if (current > maxConcurrentReceives)
            {
                maxConcurrentReceives = current;
            }
            try
            {
                var ct = callInfo.Arg<CancellationToken>();
                using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            finally
            {
                Interlocked.Decrement(ref inFlightReceives);
            }
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2500));
        var tempConfig = Path.Combine(Path.GetTempPath(), $"sat-conc-{Guid.NewGuid():N}.json");

        try
        {
            var satellite = new SatelliteEmulatorDevice(
                "http://localhost:5000",
                storage,
                tempConfig,
                null,
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
            tcs.TrySetCanceled();
            if (File.Exists(tempConfig)) File.Delete(tempConfig);
        }

        Assert.Equal(1, maxConcurrentReceives);
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
            .Returns(Result.Ok(new ReportResponse(new List<FrameResult> { new(true), new(true) })));

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
    public async Task Sleepy_battery_device_applies_settings_from_a_report_and_reports_the_value_next_wake()
    {
        var client = Substitute.For<IHomeAppsClient>();
        var mac = Substitute.For<IMacSystem>();
        var storage = new EmulatorStorage();
        var deviceId = Guid.NewGuid();
        storage.Devices["battery"] = new PairedDeviceState
        {
            DeviceId = deviceId, HouseholdId = Guid.NewGuid(), Name = "Mac Battery (test)", Secret = "sec", Slug = "mac-battery-test", Purpose = "smart_home",
        };
        client.GetDeviceTokenAsync(Arg.Any<string>(), deviceId, "sec", Arg.Any<CancellationToken>()).Returns(Result.Ok("bat-token"));
        mac.GetBatteryLevelAsync(Arg.Any<CancellationToken>()).Returns(80);

        var reports = new List<IReadOnlyList<DeviceFrame>>();
        client.SendReportAsync(Arg.Any<string>(), "bat-token", Arg.Do<IReadOnlyList<DeviceFrame>>(f => reports.Add(f)), Arg.Any<CancellationToken>())
            .Returns(
                Result.Ok(new ReportResponse([new(true), new(true)], System.Text.Json.JsonDocument.Parse("""{ "level": 3 }""").RootElement.Clone())),
                Result.Ok(new ReportResponse([new(true), new(true)])));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));
        var tempConfig = Path.Combine(Path.GetTempPath(), $"bat-set-{Guid.NewGuid():N}.json");
        try
        {
            await new BatteryEmulatorDevice("http://localhost:5000", storage, tempConfig, null, client, mac, TimeSpan.FromMilliseconds(100)).RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (File.Exists(tempConfig)) File.Delete(tempConfig);
        }

        Assert.True(reports.Count >= 2);
        Assert.DoesNotContain("level", reports[0].First(f => f.Type == "state").Values!.Value.GetRawText(), StringComparison.Ordinal);
        Assert.Equal(3, reports[1].First(f => f.Type == "state").Values!.Value.GetProperty("level").GetInt32());
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
    public async Task Satellite_handles_device_unpaired_by_resetting_local_storage()
    {
        var storage = new EmulatorStorage();
        var client = Substitute.For<IHomeAppsClient>();
        var ws = Substitute.For<IDeviceWebSocket>();
        var mac = Substitute.For<IMacSystem>();

        var deviceId = Guid.NewGuid();
        var householdId = Guid.NewGuid();

        storage.Devices["satellite"] = new PairedDeviceState
        {
            DeviceId = deviceId,
            HouseholdId = householdId,
            Name = "Mac Satellite (test)",
            Secret = "sec-unpair",
            Slug = "mac-satellite-test",
            Purpose = "assistant"
        };

        client.GetDeviceTokenAsync(Arg.Any<string>(), deviceId, "sec-unpair", Arg.Any<CancellationToken>())
            .Returns(Result.Fail("devices.device_unpaired"));

        var tempConfig = Path.Combine(Path.GetTempPath(), $"sat-unpair-{Guid.NewGuid():N}.json");
        await storage.SaveAsync(tempConfig);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        var satellite = new SatelliteEmulatorDevice(
            "http://localhost:5000",
            storage,
            tempConfig,
            pairingCode: null,
            client,
            () => ws,
            mac,
            TimeProvider.System);

        try
        {
            await satellite.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (File.Exists(tempConfig)) File.Delete(tempConfig);
        }

        Assert.False(storage.Devices.ContainsKey("satellite"));
    }

    [Fact]
    public async Task Battery_handles_device_unpaired_on_report_by_resetting_local_storage()
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
            Secret = "sec-unpair-bat",
            Slug = "mac-battery-test",
            Purpose = "smart_home"
        };

        client.GetDeviceTokenAsync(Arg.Any<string>(), deviceId, "sec-unpair-bat", Arg.Any<CancellationToken>())
            .Returns(Result.Ok("valid-token"));

        mac.GetBatteryLevelAsync(Arg.Any<CancellationToken>()).Returns(88);

        client.SendReportAsync(Arg.Any<string>(), "valid-token", Arg.Any<IReadOnlyList<DeviceFrame>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail("devices.device_unpaired"));

        var tempConfig = Path.Combine(Path.GetTempPath(), $"bat-unpair-{Guid.NewGuid():N}.json");
        await storage.SaveAsync(tempConfig);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        var battery = new BatteryEmulatorDevice(
            "http://localhost:5000",
            storage,
            tempConfig,
            pairingCode: null,
            client,
            mac);

        try
        {
            await battery.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (File.Exists(tempConfig)) File.Delete(tempConfig);
        }

        Assert.False(storage.Devices.ContainsKey("battery"));
    }

    [Fact]
    public void All_emulated_device_names_contain_test_keyword()
    {
        var storage = new EmulatorStorage();
        var client = Substitute.For<IHomeAppsClient>();
        var ws = Substitute.For<IDeviceWebSocket>();
        var mac = Substitute.For<IMacSystem>();

        var sat = new SatelliteEmulatorDevice("http://localhost", storage, "c", null, client, () => ws, mac, TimeProvider.System);
        var pow = new PoweredEmulatorDevice("http://localhost", storage, "c", null, client, () => ws, mac, TimeProvider.System);
        var bat = new BatteryEmulatorDevice("http://localhost", storage, "c", null, client, mac);
        var comp = new ComputerEmulatorDevice("http://localhost", null, () => ws, mac);

        Assert.Contains("test", sat.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test", pow.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test", bat.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test", comp.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Satellite_retries_when_client_throws_exception()
    {
        var storage = new EmulatorStorage();
        var client = Substitute.For<IHomeAppsClient>();
        var ws = Substitute.For<IDeviceWebSocket>();
        var mac = Substitute.For<IMacSystem>();

        var attempts = 0;
        client.PairAsync(Arg.Any<string>(), "FAIL_THEN_OK", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                attempts++;
                if (attempts == 1)
                {
                    throw new HttpRequestException("Network failure simulating server unavailable");
                }
                return Result.Ok(new PairResponse(Guid.NewGuid(), Guid.NewGuid(), "Satellite (test)", "sec", "slug", "assistant"));
            });

        client.GetDeviceTokenAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok("tok"));

        ws.State.Returns(WebSocketState.Open);
        ws.ReceiveMessageAsync(Arg.Any<CancellationToken>()).Returns(new LinkMessage("ready", DeviceId: Guid.NewGuid()), (LinkMessage?)null);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var tempConfig = Path.Combine(Path.GetTempPath(), $"sat-retry-{Guid.NewGuid():N}.json");

        try
        {
            var sat = new SatelliteEmulatorDevice("http://localhost", storage, tempConfig, "FAIL_THEN_OK", client, () => ws, mac, TimeProvider.System);
            await sat.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (File.Exists(tempConfig)) File.Delete(tempConfig);
        }

        Assert.True(attempts >= 1);
    }

    [Fact]
    public async Task Battery_device_omits_battery_state_when_no_battery_detected()
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
            Secret = "sec_bat",
            Slug = "mac-battery-test",
            Purpose = "smart_home"
        };

        client.GetDeviceTokenAsync(Arg.Any<string>(), deviceId, "sec_bat", Arg.Any<CancellationToken>())
            .Returns(Result.Ok("bat-token"));

        mac.GetBatteryLevelAsync(Arg.Any<CancellationToken>()).Returns((int?)null);

        IReadOnlyList<DeviceFrame>? capturedFrames = null;
        client.SendReportAsync(Arg.Any<string>(), "bat-token", Arg.Do<IReadOnlyList<DeviceFrame>>(f => capturedFrames = f), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new ReportResponse(new List<FrameResult> { new(true) })));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var tempConfig = Path.Combine(Path.GetTempPath(), $"bat-nobat-{Guid.NewGuid():N}.json");

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

        Assert.NotNull(capturedFrames);
        Assert.Single(capturedFrames);
        Assert.Equal("status", capturedFrames[0].Type);
        Assert.Null(capturedFrames[0].Battery);
    }

    [Fact]
    public async Task Satellite_handles_invalid_or_unknown_set_command()
    {
        var storage = new EmulatorStorage();
        var client = Substitute.For<IHomeAppsClient>();
        var ws = Substitute.For<IDeviceWebSocket>();
        var mac = Substitute.For<IMacSystem>();

        var deviceId = Guid.NewGuid();
        storage.Devices["satellite"] = new PairedDeviceState
        {
            DeviceId = deviceId,
            HouseholdId = Guid.NewGuid(),
            Name = "Mac Satellite (test)",
            Secret = "sec1",
            Slug = "mac-satellite-test",
            Purpose = "assistant"
        };

        client.GetDeviceTokenAsync(Arg.Any<string>(), deviceId, "sec1", Arg.Any<CancellationToken>())
            .Returns(Result.Ok("test-token"));

        ws.State.Returns(WebSocketState.Open);

        using var unknownVal = JsonDocument.Parse("\"some-val\"");

        var receiveSequence = new Queue<LinkMessage?>(new[]
        {
            new LinkMessage("ready", DeviceId: deviceId),
            new LinkMessage("set", Id: "err1", Name: "non_existent_control", Value: unknownVal.RootElement),
            null
        });

        ws.ReceiveMessageAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(receiveSequence.Count > 0 ? receiveSequence.Dequeue() : null));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var tempConfig = Path.Combine(Path.GetTempPath(), $"sat-unknown-{Guid.NewGuid():N}.json");

        try
        {
            var satellite = new SatelliteEmulatorDevice(
                "http://localhost:5000",
                storage,
                tempConfig,
                null,
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

        await ws.Received().SendMessageAsync(Arg.Is<LinkMessage>(m => m.Type == "set_result" && m.Id == "err1" && m.Ok == false), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Computer_device_connects_with_user_token_and_does_not_send_state()
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

        // Computer must never send state frame (unsupported on server for computer devices)
        await ws.DidNotReceive().SendMessageAsync(Arg.Is<LinkMessage>(m => m.Type == "state"), Arg.Any<CancellationToken>());
    }
}
