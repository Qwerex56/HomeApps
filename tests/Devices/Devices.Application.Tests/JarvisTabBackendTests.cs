using System.Text.Json;
using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Application.Frames;
using Devices.Application.Frames.ProcessFrames;
using Devices.Application.Heartbeat.CheckHeartbeats;
using Devices.Application.HouseholdDevices.GetHouseholdDevices;
using Devices.Application.HouseholdDevices.PairDevice;
using Devices.Application.HouseholdDevices.RenameDevice;
using Devices.Application.HouseholdDevices.SetAdminNotes;
using Devices.Application.HouseholdDevices.SetJarvisAccess;
using Devices.Application.Links.ConnectDevice;
using Devices.Application.MyDevices.RemoveDevice;
using Devices.Application.MyDevices.RestoreDevice;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Capabilities;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.Contracts.Households;
using Microsoft.Extensions.Logging.Abstractions;

namespace Devices.Application.Tests;

/// <summary>H-9 backend extensions for the JARVIS tab: extended descriptor view, status.lowBattery and changed stream messages.</summary>
public sealed class JarvisTabBackendTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

    private const string FullChair = """
        { "format": "homeapps.device/1", "model": "a/fotel", "version": "1.0.0", "purpose": "smart_home", "heartbeat": 60, "sleepy": true,
          "jarvis": "Fotel w salonie, grzanie 3 tylko na krótko.",
          "controls": [ { "name": "heating", "label": "Grzanie", "description": "0 = wyłączone", "value": { "type": "integer", "minimum": 0, "maximum": 3 }, "widget": "select" } ],
          "sensors": [ { "name": "temperature", "label": "Temperatura", "value": { "type": "number" }, "unit": "°C" } ],
          "events": [ { "name": "button", "label": "Przycisk", "notify": true }, { "name": "tag", "label": "Znacznik", "location": true } ] }
        """;

    private readonly Guid _household = Guid.NewGuid();
    private readonly Guid _admin = Guid.NewGuid();
    private readonly FakeDevices _devices = new();
    private readonly FakeTelemetry _telemetry = new();
    private readonly FakeLinks _links = new();
    private readonly FakeStream _stream = new();
    private readonly FakeBus _bus = new();
    private readonly FakeTime _time = new(Now);

    [Theory]
    [InlineData(HouseholdRole.Guest, false)]
    [InlineData(HouseholdRole.Member, false)]
    [InlineData(HouseholdRole.Admin, true)]
    [InlineData(HouseholdRole.Owner, true)]
    public async Task DeviceDescriptorView_includes_controls_sensors_events_and_jarvis_for_admins_only(HouseholdRole role, bool seesJarvis)
    {
        var described = PairedDevice.Create(Now, FullChair, _household);
        described.SetAdminNotes("Fotel taty");
        var bare = PairedDevice.Create(Now, descriptor: null, _household);
        _devices.Add(described);
        _devices.Add(bare);

        var list = (await new GetHouseholdDevicesHandler(_devices, new FakeRoles(role, _household), _links, _telemetry)
            .Handle(new GetHouseholdDevicesQuery(_household), CancellationToken.None)).Value;

        var full = list.Single(d => d.Id == described.Id);
        Assert.True(full.Descriptor.Described);
        Assert.True(full.Descriptor.Sleepy);
        var control = Assert.Single(full.Descriptor.Controls);
        Assert.Equal(("heating", "Grzanie", "0 = wyłączone", "select", "integer"),
            (control.Name, control.Label, control.Description, control.Widget, control.Value.GetProperty("type").GetString()));
        var sensor = Assert.Single(full.Descriptor.Sensors);
        Assert.Equal(("temperature", "Temperatura", "°C", "number"),
            (sensor.Name, sensor.Label, sensor.Unit, sensor.Value.GetProperty("type").GetString()));
        Assert.Equal([("button", "Przycisk", true, false), ("tag", "Znacznik", false, true)],
            full.Descriptor.Events.Select(e => (e.Name, e.Label, e.Notify, e.IsLocation)));
        Assert.Equal(seesJarvis ? "Fotel w salonie, grzanie 3 tylko na krótko." : null, full.Descriptor.Jarvis);
        Assert.Equal(seesJarvis ? "Fotel taty" : null, full.AdminNotes);

        var legacy = list.Single(d => d.Id == bare.Id);
        Assert.False(legacy.Descriptor.Described);
        Assert.NotNull(legacy.Descriptor.Controls);
        Assert.Empty(legacy.Descriptor.Controls);
        Assert.NotNull(legacy.Descriptor.Sensors);
        Assert.Empty(legacy.Descriptor.Sensors);
        Assert.NotNull(legacy.Descriptor.Events);
        Assert.Empty(legacy.Descriptor.Events);
        Assert.Null(legacy.Descriptor.Jarvis);
    }

    [Fact]
    public async Task Stream_status_carries_lowBattery_on_report_and_when_going_silent()
    {
        var device = PairedDevice.Create(Now, FullChair, _household);
        _devices.Add(device);
        var notices = new AdminNotices(new FakeDirectory(), _bus, NullLogger<AdminNotices>.Instance);
        var frames = new ProcessFramesHandler(_devices, _telemetry, _stream, _bus, notices, new RecentControlSources(_time), _time);

        await frames.Handle(new ProcessFramesCommand(device.Id, [new DeviceFrame("status", Battery: 15)]), CancellationToken.None);

        var reported = JsonSerializer.SerializeToElement(_stream.Messages.Single().Data);
        Assert.True(reported.GetProperty("lowBattery").GetBoolean());
        Assert.False(reported.GetProperty("silent").GetBoolean());

        _stream.Messages.Clear();
        _time.Now = Now.AddMinutes(10);
        await new CheckHeartbeatsHandler(_devices, _telemetry, _stream, _bus, notices, _time)
            .Handle(new CheckHeartbeatsCommand(Now), CancellationToken.None);

        var silent = JsonSerializer.SerializeToElement(_stream.Messages.Single().Data);
        Assert.True(silent.GetProperty("silent").GetBoolean());
        Assert.True(silent.GetProperty("lowBattery").GetBoolean());
    }

    [Fact]
    public async Task Stream_changed_is_published_for_rename_notes_jarvis_remove_restore_pair_and_descriptor()
    {
        var code = PairingCode.New();
        var added = Device.AddToHousehold(_household, _admin, "Lampa", PairingCode.Hash(code), [], Now).Value;
        _devices.Add(added);
        var roles = new FakeRoles(HouseholdRole.Admin, _household);
        var adminUser = new FakeUser(_admin);
        var intake = new DescriptorIntake(new EmptyRegistry(), _time);

        await new PairDeviceHandler(_devices, new OkIdentities(), intake, _stream, _time)
            .Handle(new PairDeviceCommand(code, FullChair), CancellationToken.None);
        await new RenameDeviceHandler(_devices, roles, _stream, adminUser)
            .Handle(new RenameDeviceCommand(added.Id, "Fotel taty"), CancellationToken.None);
        await new SetAdminNotesHandler(_devices, roles, _stream, adminUser)
            .Handle(new SetAdminNotesCommand(added.Id, "Uwaga"), CancellationToken.None);
        await new SetJarvisAccessHandler(_devices, roles, _stream, adminUser)
            .Handle(new SetJarvisAccessCommand(added.Id, false, false), CancellationToken.None);
        await new ConnectDeviceHandler(_devices, intake, new FakeUser(added.Id), _stream, _time)
            .Handle(new ConnectDeviceCommand("Fotel taty", DeviceKind.Satellite, [], FullChair), CancellationToken.None);
        await new RemoveDeviceHandler(_devices, roles, _links, _stream, _bus, adminUser, _time)
            .Handle(new RemoveDeviceCommand(added.Id), CancellationToken.None);
        await new RestoreDeviceHandler(_devices, roles, _stream, adminUser)
            .Handle(new RestoreDeviceCommand(added.Id), CancellationToken.None);

        var reasons = _stream.Messages
            .Where(m => m.Type == "changed")
            .Select(m => JsonSerializer.SerializeToElement(m.Data).GetProperty("reason").GetString()!)
            .ToArray();
        Assert.Equal(["paired", "renamed", "notes", "jarvis", "descriptor", "removed", "restored"], reasons);
    }

    private sealed class EmptyRegistry : ICapabilityRegistry
    {
        public Task<RegisteredCapability?> FindAsync(string name, CancellationToken cancellationToken) => Task.FromResult<RegisteredCapability?>(null);
        public Task<IReadOnlyList<RegisteredCapability>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<RegisteredCapability>>([]);
        public void Add(RegisteredCapability capability) { }
    }

    private sealed class OkIdentities : ISatelliteIdentities
    {
        public Task<Result<string>> PairAsync(Guid deviceId, Guid householdId, string name, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Ok("secret"));
    }
}
