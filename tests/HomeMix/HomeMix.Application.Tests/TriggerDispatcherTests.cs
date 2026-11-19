using System.Text.Json;
using HomeApps.Contracts.Households;
using HomeApps.Contracts.Notifications;
using HomeMix.Application.Installations;
using HomeMix.Application.Triggers;
using HomeMix.Application.Triggers.ListHouseholdTriggers;
using HomeMix.Application.Triggers.SetTriggerEnabled;
using HomeMix.Domain.Devices;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Packages;
using HomeMix.Domain.Triggers;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeMix.Application.Tests;

public sealed class TriggerDispatcherTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Adam = Guid.NewGuid();
    private static readonly Guid HouseholdId = Guid.NewGuid();

    private const string Manifest = """
        {
          "homemixVersion": 1, "id": "termostat", "version": "1.0.0", "requires": { "homeApps": "2.3.0" },
          "name": { "pl": "Termostat" }, "entry": "index.html", "targets": ["Household"],
          "capabilities": ["devices", "devices.control"],
          "triggers": [
            { "on": "state", "capability": "weather", "field": "temperature", "script": "triggers/cold.js" },
            { "on": "event", "role": "doorbell", "name": "pressed", "script": "triggers/bell.js" }
          ]
        }
        """;

    private static readonly DeviceCandidate Station = new(Guid.NewGuid(), "Stacja", true, "a/stacja", ["weather_station"], ["weather", "power"]);
    private static readonly DeviceCandidate Bell = new(Guid.NewGuid(), "Dzwonek", true, null, ["doorbell"], ["button"]);

    private readonly FakeTriggerScripts _scripts = new();
    private readonly FakeTriggerStates _states = new();
    private readonly TriggerBus _bus = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly ClockAt _time = new(Now);
    private readonly Installation _installation;
    private readonly AddonPackage _package;
    private readonly Guid _otherAdmin = Guid.NewGuid();

    public TriggerDispatcherTests()
    {
        _package = Package(Manifest);
        _installation = Install(_package);
    }

    private static AddonPackage Package(string manifest) => AddonPackage.Create(
        Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(manifest))), manifest,
        [new PackageFile("homemix.json", 100), new PackageFile("index.html", 10), new PackageFile("triggers/cold.js", 10), new PackageFile("triggers/bell.js", 10)],
        1024, null, null, Adam, Now).Value;

    private static Installation Install(AddonPackage package) =>
        Installation.Install(package, InstallTarget.Household(HouseholdId), new Consent(package.Manifest.Capabilities, [], false), Adam, Now).Value;

    private TriggerDispatcher Dispatcher(TriggerRunLimiter? limiter = null, params Installation[] installations) => new(
        new TriggerInstallations(installations.Length == 0 ? [_installation] : installations), new TriggerPackages(_package), FakeBlocks.Blocklist(),
        _states, _scripts, new FakeAdmins(Adam, _otherAdmin), limiter ?? new TriggerRunLimiter(_time), _bus, _unitOfWork, _time, NullLogger<TriggerDispatcher>.Instance);

    private static TriggerOccurrence Changed(string json, DeviceCandidate? device = null, string? changedBy = null) => new(
        HouseholdId, device ?? Station, "stacja", TriggerKind.State, JsonDocument.Parse(json).RootElement.Clone(), null, null, changedBy, Now);

    private static TriggerOccurrence Pressed(string name = "pressed") => new(
        HouseholdId, Bell, "dzwonek", TriggerKind.Event, null, name, JsonDocument.Parse("""{"kind":"single"}""").RootElement.Clone(), null, Now);

    [Fact]
    public async Task A_matching_state_change_runs_the_trigger_and_remembers_the_run()
    {
        await Dispatcher().DispatchAsync(Changed("""{"temperature":18.5,"humidity":40}"""), CancellationToken.None);

        var run = Assert.Single(_scripts.Runs);
        Assert.Equal("cold", run.Trigger.Id);
        Assert.Equal(_installation.Id, run.Installation.Id);
        var state = Assert.Single(_states.All);
        Assert.Equal((Now, null, 0), (state.LastRunAt, state.LastError, state.ConsecutiveFailures));
        Assert.Equal(1, _unitOfWork.Saves);
    }

    [Fact]
    public async Task Other_fields_other_devices_and_other_kinds_run_nothing()
    {
        var dispatcher = Dispatcher();

        await dispatcher.DispatchAsync(Changed("""{"humidity":40}"""), CancellationToken.None);
        await dispatcher.DispatchAsync(Changed("""{"temperature":18}""", Bell), CancellationToken.None);
        await dispatcher.DispatchAsync(Pressed("motion"), CancellationToken.None);

        Assert.Empty(_scripts.Runs);
        Assert.Equal(0, _unitOfWork.Saves);
    }

    [Fact]
    public async Task An_event_runs_the_event_trigger_with_the_occurrence()
    {
        await Dispatcher().DispatchAsync(Pressed(), CancellationToken.None);

        var run = Assert.Single(_scripts.Runs);
        Assert.Equal("bell", run.Trigger.Id);
        Assert.Equal("pressed", run.Occurrence.EventName);
        Assert.Equal("single", run.Occurrence.Payload!.Value.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task A_household_without_the_add_on_or_an_add_on_without_devices_runs_nothing()
    {
        var elsewhere = new TriggerOccurrence(Guid.NewGuid(), Station, "stacja", TriggerKind.State, JsonDocument.Parse("""{"temperature":1}""").RootElement.Clone(), null, null, null, Now);
        await Dispatcher().DispatchAsync(elsewhere, CancellationToken.None);

        var withoutDevices = Package(Manifest.Replace("\"devices\", \"devices.control\"", "\"storage.household\"", StringComparison.Ordinal)
            .Replace("\"triggers\"", "\"_t\"", StringComparison.Ordinal));
        await Dispatcher(null, Install(withoutDevices)).DispatchAsync(Changed("""{"temperature":1}"""), CancellationToken.None);

        Assert.Empty(_scripts.Runs);
    }

    [Fact]
    public async Task A_change_the_installation_made_itself_does_not_fire_it_again_but_another_source_does()
    {
        var dispatcher = Dispatcher();

        await dispatcher.DispatchAsync(Changed("""{"temperature":18}""", changedBy: TriggerSource.For(_installation.Id)), CancellationToken.None);
        Assert.Empty(_scripts.Runs);

        await dispatcher.DispatchAsync(Changed("""{"temperature":18}""", changedBy: TriggerSource.For(Guid.NewGuid())), CancellationToken.None);
        Assert.Single(_scripts.Runs);
    }

    [Fact]
    public async Task An_installation_runs_at_most_thirty_times_a_minute()
    {
        var dispatcher = Dispatcher();

        for (var i = 0; i < 35; i++)
        {
            await dispatcher.DispatchAsync(Changed("""{"temperature":18}"""), CancellationToken.None);
        }

        Assert.Equal(TriggerRunLimiter.PerMinute, _scripts.Runs.Count);

        _time.Now += TimeSpan.FromSeconds(61);
        await dispatcher.DispatchAsync(Changed("""{"temperature":18}"""), CancellationToken.None);
        Assert.Equal(TriggerRunLimiter.PerMinute + 1, _scripts.Runs.Count);
    }

    [Fact]
    public async Task The_limiter_counts_per_installation()
    {
        var limiter = new TriggerRunLimiter(_time);
        var (first, second) = (Guid.NewGuid(), Guid.NewGuid());

        for (var i = 0; i < TriggerRunLimiter.PerMinute; i++)
        {
            Assert.True(limiter.TryAcquire(first));
        }

        Assert.False(limiter.TryAcquire(first));
        Assert.True(limiter.TryAcquire(second));
        await Task.CompletedTask;
    }

    [Fact]
    public void The_limiter_window_resets_with_time()
    {
        var limiter = new TriggerRunLimiter(_time);
        var id = Guid.NewGuid();
        for (var i = 0; i < TriggerRunLimiter.PerMinute; i++)
        {
            Assert.True(limiter.TryAcquire(id));
        }

        _time.Now += TimeSpan.FromSeconds(59);
        Assert.False(limiter.TryAcquire(id));
        _time.Now += TimeSpan.FromSeconds(1);
        Assert.True(limiter.TryAcquire(id));
    }

    [Fact]
    public async Task A_success_resets_the_failure_count()
    {
        var dispatcher = Dispatcher();
        _scripts.Fails = "boom";
        for (var i = 0; i < 3; i++)
        {
            await dispatcher.DispatchAsync(Changed("""{"temperature":18}"""), CancellationToken.None);
        }

        Assert.Equal(3, Assert.Single(_states.All).ConsecutiveFailures);
        _scripts.Fails = null;
        await dispatcher.DispatchAsync(Changed("""{"temperature":18}"""), CancellationToken.None);

        var state = Assert.Single(_states.All);
        Assert.Equal(0, state.ConsecutiveFailures);
        Assert.Null(state.LastError);
    }

    [Fact]
    public async Task Removing_an_installation_forgets_its_trigger_states()
    {
        _scripts.Fails = "boom";
        var other = Install(_package);
        await Dispatcher(null, _installation, other).DispatchAsync(Changed("""{"temperature":18}"""), CancellationToken.None);
        Assert.Contains(_states.All, s => s.InstallationId == _installation.Id);
        Assert.Contains(_states.All, s => s.InstallationId == other.Id);

        var access = new InstallationAccess(new FakeRoles(HouseholdRole.Admin), new FakeUser(Adam));
        var handler = new HomeMix.Application.Installations.RemoveInstallation.RemoveInstallationHandler(
            new TriggerInstallations(_installation), new TriggerPackages(), access, new FakeUser(Adam), _time, new HomeMix.Application.Realtime.RealtimeOutbox(),
            NSubstitute.Substitute.For<HomeMix.Application.Abstractions.IStoreStatsRepository>(), _states);
        var result = await handler.Handle(new HomeMix.Application.Installations.RemoveInstallation.RemoveInstallationCommand(_installation.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(_installation.IsActive);
        Assert.DoesNotContain(_states.All, s => s.InstallationId == _installation.Id);
        Assert.Contains(_states.All, s => s.InstallationId == other.Id);
    }

    [Fact]
    public async Task Five_failures_in_a_row_turn_the_trigger_off_and_tell_the_admins_once()
    {
        _scripts.Fails = "boom";
        var dispatcher = Dispatcher();

        for (var i = 0; i < 8; i++)
        {
            await dispatcher.DispatchAsync(Changed("""{"temperature":18}"""), CancellationToken.None);
        }

        Assert.Equal(TriggerState.MaxFailures, _scripts.Runs.Count);
        var state = Assert.Single(_states.All);
        Assert.False(state.Enabled);
        Assert.Equal("boom", state.LastError);
        var notice = Assert.IsType<NotificationRequested>(Assert.Single(_bus.Published));
        Assert.Equal([Adam, _otherAdmin], notice.UserIds);
        Assert.Equal(HouseholdId, notice.HouseholdId);
        Assert.Contains("cold", notice.Body, StringComparison.Ordinal);
        Assert.Contains("boom", notice.Body, StringComparison.Ordinal);

        // The other trigger of the same add-on keeps working.
        await dispatcher.DispatchAsync(Pressed(), CancellationToken.None);
        Assert.Equal("bell", _scripts.Runs[^1].Trigger.Id);
    }

    [Fact]
    public async Task A_success_in_between_keeps_the_trigger_on()
    {
        var dispatcher = Dispatcher();
        for (var i = 0; i < 4; i++)
        {
            _scripts.Fails = "boom";
            await dispatcher.DispatchAsync(Changed("""{"temperature":18}"""), CancellationToken.None);
        }

        _scripts.Fails = null;
        await dispatcher.DispatchAsync(Changed("""{"temperature":18}"""), CancellationToken.None);
        _scripts.Fails = "boom";
        await dispatcher.DispatchAsync(Changed("""{"temperature":18}"""), CancellationToken.None);

        Assert.True(Assert.Single(_states.All).Enabled);
        Assert.Empty(_bus.Published);
    }

    [Fact]
    public async Task An_admin_lists_and_switches_triggers_and_a_member_cannot()
    {
        var installations = new TriggerInstallations(_installation);
        var packages = new TriggerPackages(_package);
        var admin = new InstallationAccess(new FakeRoles(HouseholdRole.Admin), new FakeUser(Adam));
        var member = new InstallationAccess(new FakeRoles(HouseholdRole.Member), new FakeUser(Adam));

        var off = await new SetTriggerEnabledHandler(installations, packages, _states, admin)
            .Handle(new SetTriggerEnabledCommand(_installation.Id, "cold", false), CancellationToken.None);
        Assert.False(off.Value.Enabled);
        _scripts.Fails = null;
        await Dispatcher().DispatchAsync(Changed("""{"temperature":18}"""), CancellationToken.None);
        Assert.Empty(_scripts.Runs);

        var listed = (await new ListHouseholdTriggersHandler(installations, packages, _states, admin)
            .Handle(new ListHouseholdTriggersQuery(HouseholdId), CancellationToken.None)).Value;
        Assert.Equal(["bell", "cold"], listed.Select(t => t.Id));
        Assert.False(listed.Single(t => t.Id == "cold").Enabled);
        Assert.True(listed.Single(t => t.Id == "bell").Enabled);

        var on = await new SetTriggerEnabledHandler(installations, packages, _states, admin)
            .Handle(new SetTriggerEnabledCommand(_installation.Id, "cold", true), CancellationToken.None);
        Assert.True(on.Value.Enabled);

        Assert.Equal("homemix.forbidden", ((HomeApps.BuildingBlocks.Domain.Errors.AppError)(await new ListHouseholdTriggersHandler(installations, packages, _states, member)
            .Handle(new ListHouseholdTriggersQuery(HouseholdId), CancellationToken.None)).Errors[0]).Code);
        Assert.Equal("homemix.forbidden", ((HomeApps.BuildingBlocks.Domain.Errors.AppError)(await new SetTriggerEnabledHandler(installations, packages, _states, member)
            .Handle(new SetTriggerEnabledCommand(_installation.Id, "cold", false), CancellationToken.None)).Errors[0]).Code);
        Assert.Equal("homemix.trigger_not_found", ((HomeApps.BuildingBlocks.Domain.Errors.AppError)(await new SetTriggerEnabledHandler(installations, packages, _states, admin)
            .Handle(new SetTriggerEnabledCommand(_installation.Id, "nope", true), CancellationToken.None)).Errors[0]).Code);
    }
}
