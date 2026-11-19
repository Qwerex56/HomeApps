using System.Text.Json;
using FluentResults;
using HomeApps.Contracts.Households;
using HomeMix.Api.Mcp;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Triggers;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Triggers;
using Jint;
using Jint.Native.Function;

namespace HomeMix.Api.Triggers;

/// <summary>
/// Runs a trigger's script, the add-on's <c>triggers/{name}.js</c>, in Jint like a script tool and within the same
/// <see cref="ScriptLimits"/>: the module exports <c>export default async function (trigger, ctx)</c>. Its <c>ctx</c> is the tool's
/// <c>context</c> and <c>can</c> plus <c>trigger</c> and <c>devices</c>; no data, members or fetch, because nobody is logged in and the
/// script acts as the add-on within its consent and the installer's rights (household-jarvis-devices.md 2.6).
/// </summary>
internal sealed class TriggerScriptRunner(ScriptRunner scripts, IAddonDevices devices) : ITriggerScripts
{
    private const string Bootstrap = """
        const ctx = (() => {
          const host = __host
          const call = (method, ...args) => {
            const answer = JSON.parse(host(method, JSON.stringify(args)))
            if (answer.error) {
              const error = new Error(answer.error.message)
              error.code = answer.error.code
              return Promise.reject(error)
            }
            return Promise.resolve(answer.value)
          }
          const context = Object.freeze(JSON.parse(__context))
          return Object.freeze({
            context: () => Promise.resolve(context),
            can: (permission) => Promise.resolve(context.permissions.includes(permission)),
            trigger: Object.freeze(JSON.parse(__trigger)),
            devices: Object.freeze({
              list: (filter) => call('devices.list', filter ?? {}),
              state: (id) => call('devices.state', id),
              set: (id, control, value) => call('devices.set', id, control, value).then(() => undefined),
            }),
          })
        })()
        """;

    private static readonly JsonSerializerOptions Json = ScriptBridge.Json;

    public async Task<Result> RunAsync(TriggerRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        var source = await scripts.ReadAsync(run.Installation.PackageSha256, run.Trigger.Script, cancellationToken);
        if (source is null)
        {
            return Result.Fail($"{run.Trigger.Script} is missing or larger than {ScriptLimits.SourceBytes / 1024} KB.");
        }

        var context = Context(run);
        var trigger = Info(run);

        // Jint runs synchronously and its ctx calls wait for Devices, so the run gets a thread of its own.
        return await Task.Run(() => Run(run, source, context, trigger, cancellationToken), cancellationToken);
    }

    private Result Run(TriggerRun run, string source, string context, string trigger, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(ScriptLimits.Time);
        var session = new TriggerSession(run, devices, limit.Token);
        using var engine = new Engine(o => o
            .LimitMemory(ScriptLimits.Memory)
            .MaxStatements(ScriptLimits.Statements)
            .LimitRecursion(ScriptLimits.Recursion)
            .CancellationToken(limit.Token));
        try
        {
            engine.SetValue("__host", new Func<string, string, string>(session.Call));
            engine.SetValue("__context", context);
            engine.SetValue("__trigger", trigger);
            engine.Execute(Bootstrap);
            engine.Modules.Add("trigger", source);
            var export = engine.Modules.Import("trigger").Get("default");
            if (export is not Function)
            {
                return Result.Fail($"{run.Trigger.Script} must export a default function (trigger, ctx).");
            }

            engine.SetValue("__run", export);
            engine.Evaluate("Promise.resolve(__run(ctx.trigger, ctx)).then(() => undefined)").UnwrapIfPromise(limit.Token);
            return Result.Ok();
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested && ScriptRunner.Problem(e) is { } problem)
        {
            return Result.Fail(problem);
        }
    }

    /// <summary>As the tool's <c>ctx.context()</c>, with the installer as the user (an Admin at least, or the trigger would not run).</summary>
    private static string Context(TriggerRun run)
    {
        var installation = run.Installation;
        var permissions = run.Manifest.Permissions
            .Where(p => Enum.TryParse<HouseholdRole>(p.MinRole, out var min) && HouseholdRole.Admin >= min)
            .Select(p => p.Id);
        return JsonSerializer.Serialize(new
        {
            addon = new { id = run.Manifest.Id, name = run.Manifest.Name.Pl, version = run.Manifest.Version },
            installation = new { id = installation.Id, target = "Household" },
            household = new { id = installation.TargetId, name = string.Empty, myRole = nameof(HouseholdRole.Admin) },
            user = new { id = installation.InstalledBy, name = string.Empty },
            permissions,
            capabilities = installation.Capabilities,
            language = "pl",
            theme = new { dark = false, vars = new { } },
            page = string.Empty,
            widget = (object?)null,
        }, Json);
    }

    private static string Info(TriggerRun run)
    {
        var o = run.Occurrence;
        var device = new { id = o.Device.Id, name = o.Device.Name, slug = o.Slug, model = o.Device.Model, roles = o.Device.Roles, capabilities = o.Device.Capabilities };
        return o.Kind == TriggerKind.State
            ? JsonSerializer.Serialize(new { id = run.Trigger.Id, on = "state", device, changed = o.Changed, at = o.At }, Json)
            : JsonSerializer.Serialize(new { id = run.Trigger.Id, on = "event", device, @event = new { name = o.EventName, payload = o.Payload }, at = o.At }, Json);
    }
}
