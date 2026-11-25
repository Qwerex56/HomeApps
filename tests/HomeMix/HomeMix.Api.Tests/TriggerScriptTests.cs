using System.Text;
using System.Text.Json;
using FluentResults;
using HomeMix.Api.Mcp;
using HomeMix.Api.Triggers;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Triggers;
using HomeMix.Domain.Devices;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Packages;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeMix.Api.Tests;

/// <summary>Trigger scripts in Jint with a fake Devices client (docs/homemix.md, "Triggers"); needs no Docker.</summary>
public sealed class TriggerScriptTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Installer = Guid.NewGuid();
    private static readonly Guid HouseholdId = Guid.NewGuid();

    private const string Cold = """
        export default async function (trigger, ctx) {
          if (trigger.changed.temperature < 19) {
            const [heater] = await ctx.devices.list({ capability: 'heating' })
            await ctx.devices.set(heater.id, 'level', 2)
          }
        }
        """;

    private readonly FakeAddonDevices _devices = new();

    private static string Manifest(string capabilities) =>
        $$"""
        {
          "homemixVersion": 1, "id": "termostat", "version": "1.0.0", "requires": { "homeApps": "2.3.0" },
          "name": { "pl": "Termostat" }, "entry": "index.html", "targets": ["Household"], "capabilities": {{capabilities}},
          "triggers": [ { "on": "state", "capability": "weather", "field": "temperature", "script": "triggers/cold.js" } ]
        }
        """;

    private (TriggerScriptRunner Runner, TriggerRun Run) Setup(string script, string json, string capabilities = """["devices", "devices.control"]""")
    {
        var manifest = Manifest(capabilities);
        var package = AddonPackage.Create(
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(manifest + script))), manifest,
            [new PackageFile("homemix.json", 100), new PackageFile("index.html", 10), new PackageFile("triggers/cold.js", 10)], 1024, null, null, Installer, Now).Value;
        var installation = Installation.Install(package, InstallTarget.Household(HouseholdId), new Consent(package.Manifest.Capabilities, [], false), Installer, Now).Value;
        var station = new DeviceCandidate(Guid.NewGuid(), "Stacja", true, "a/stacja", ["weather_station"], ["weather"]);
        var occurrence = new TriggerOccurrence(
            HouseholdId, station, "stacja", TriggerKind.State, JsonElement.Parse(json), null, null, null, Now);
        var runner = new TriggerScriptRunner(new ScriptRunner(new ScriptFiles(script), null!, NullLogger<ScriptRunner>.Instance), _devices);
        return (runner, new TriggerRun(installation, package.Manifest, package.Manifest.Triggers[0], occurrence));
    }

    private static string Problem(Result result) => Assert.Single(result.Errors).Message;

    [Fact]
    public async Task A_cold_reading_sets_the_heating_as_the_add_on()
    {
        var (runner, run) = Setup(Cold, """{"temperature":18.5}""");

        var result = await runner.RunAsync(run, CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailed ? Problem(result) : null);
        var set = Assert.Single(_devices.Sets);
        Assert.Equal((FakeAddonDevices.Heater.Id, "level", "2", run.Installation.Id, "Termostat"), (set.Device, set.Control, set.Value, set.Installation.Id, set.AddonName));
        Assert.Equal(Installer, run.Installation.InstalledBy);
    }

    [Fact]
    public async Task A_warm_reading_changes_nothing()
    {
        var (runner, run) = Setup(Cold, """{"temperature":21}""");

        Assert.True((await runner.RunAsync(run, CancellationToken.None)).IsSuccess);
        Assert.Empty(_devices.Sets);
    }

    [Fact]
    public async Task Setting_without_devices_control_fails_the_run_and_sends_nothing()
    {
        var (runner, run) = Setup(Cold, """{"temperature":10}""", capabilities: """["devices"]""");

        var result = await runner.RunAsync(run, CancellationToken.None);

        Assert.Contains("homemix.capability_missing", Problem(result), StringComparison.Ordinal);
        Assert.Empty(_devices.Sets);
    }

    [Fact]
    public async Task A_refusal_from_Devices_fails_the_run_with_its_code()
    {
        _devices.Refuse = "devices.forbidden";
        var (runner, run) = Setup(Cold, """{"temperature":10}""");

        var result = await runner.RunAsync(run, CancellationToken.None);

        Assert.Contains("devices.forbidden", Problem(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_script_sees_the_trigger_the_installer_and_the_devices_with_their_state()
    {
        var script = """
            export default async function (trigger, ctx) {
              const me = await ctx.context()
              const all = await ctx.devices.list({ role: 'heater', state: true })
              const state = await ctx.devices.state(all[0].id)
              if (trigger.id !== 'cold' || trigger.on !== 'state' || trigger.device.slug !== 'stacja' || trigger.device.capabilities[0] !== 'weather'
                  || trigger.changed.temperature !== 17 || ctx.trigger !== trigger) throw new Error('bad trigger ' + JSON.stringify(trigger))
              if (me.user.id !== '%INSTALLER%' || me.household.id !== '%HOUSEHOLD%' || me.addon.id !== 'termostat') throw new Error('bad context')
              if (all.length !== 1 || all[0].state.level !== 0 || state.level !== 0) throw new Error('bad devices')
              if (typeof ctx.data !== 'undefined' || typeof ctx.fetch !== 'undefined') throw new Error('triggers have no data or fetch')
            }
            """.Replace("%INSTALLER%", Installer.ToString(), StringComparison.Ordinal).Replace("%HOUSEHOLD%", HouseholdId.ToString(), StringComparison.Ordinal);
        var (runner, run) = Setup(script, """{"temperature":17}""");

        var result = await runner.RunAsync(run, CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailed ? Problem(result) : null);
    }

    [Fact]
    public async Task An_event_trigger_gets_the_event_and_its_payload()
    {
        var (runner, run) = Setup("""
            export default async function (trigger) {
              if (trigger.on !== 'event' || trigger.event.name !== 'pressed' || trigger.event.payload.kind !== 'long') throw new Error('bad')
            }
            """, "{}");
        var occurrence = run.Occurrence with
        {
            Kind = TriggerKind.Event, Changed = null, EventName = "pressed", Payload = JsonElement.Parse("""{"kind":"long"}"""),
        };

        var result = await runner.RunAsync(run with { Occurrence = occurrence }, CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailed ? Problem(result) : null);
    }

    [Theory]
    [InlineData("export default async function () { throw new Error('oops') }", "oops")]
    [InlineData("export default async function () { while (true) {} }", "statements")]
    [InlineData("export default 5", "must export a default function")]
    [InlineData("export default async function (t, ctx) { await ctx.devices.set('not-a-guid', 'level', 1) }", "must be a device id")]
    [InlineData("export default async function (t, ctx) { await ctx.devices.set('%ID%', 'level') }", "A value is required")]
    public async Task A_script_that_fails_or_overruns_fails_the_run(string script, string problem)
    {
        var (runner, run) = Setup(script.Replace("%ID%", FakeAddonDevices.Heater.Id.ToString(), StringComparison.Ordinal), """{"temperature":1}""");

        var result = await runner.RunAsync(run, CancellationToken.None);

        Assert.Contains(problem, Problem(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_host_call_with_malformed_arguments_fails_the_run_instead_of_throwing()
    {
        var (runner, run) = Setup(
            "export default async function () { const a = JSON.parse(__host('devices.set', 'x')); if (a.error) throw new Error(a.error.code) }", """{"temperature":1}""");

        var result = await runner.RunAsync(run, CancellationToken.None);

        Assert.Contains("homemix.bad_request", Problem(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_missing_script_fails_the_run()
    {
        var (runner, run) = Setup(Cold, """{"temperature":1}""");
        var empty = new TriggerScriptRunner(new ScriptRunner(new ScriptFiles(null), null!, NullLogger<ScriptRunner>.Instance), _devices);

        Assert.Contains("is missing", Problem(await empty.RunAsync(run, CancellationToken.None)), StringComparison.Ordinal);
        Assert.NotNull(runner);
    }

    private sealed class ScriptFiles(string? script) : IPackageStore
    {
        public Task SaveAsync(string sha256, byte[] zip, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Stream?> OpenFileAsync(string sha256, string path, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(script is null ? null : new MemoryStream(Encoding.UTF8.GetBytes(script)));
    }

    private sealed class FakeAddonDevices : IAddonDevices
    {
        public static readonly HouseholdDevice Heater = new(
            Guid.NewGuid(), "Fotel", "fotel", "smart_home", true, "a/fotel", ["heater"], ["heating"], JsonElement.Parse("""{"level":0}"""));

        public List<(Guid Device, string Control, string Value, Installation Installation, string AddonName)> Sets { get; } = [];

        public string? Refuse { get; set; }

        public Task<IReadOnlyList<HouseholdDevice>> ListAsync(Guid householdId, Guid asUser, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HouseholdDevice>>(householdId == HouseholdId && asUser == Installer ? [Heater] : []);

        public Task<Result> SetAsync(
            Installation installation, string addonName, Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken)
        {
            if (Refuse is not null)
            {
                return Task.FromResult(Result.Fail(new HomeApps.BuildingBlocks.Domain.Errors.AppError(Refuse, "Refused.", HomeApps.BuildingBlocks.Domain.Errors.ErrorKind.Forbidden)));
            }

            Sets.Add((deviceId, control, value.GetRawText(), installation, addonName));
            return Task.FromResult(Result.Ok());
        }
    }
}
