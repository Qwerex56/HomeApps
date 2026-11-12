using System.Text;
using System.Text.Json;
using FluentResults;
using HomeMix.Application.Abstractions;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Runtime;

namespace HomeMix.Api.Mcp;

/// <summary>
/// Runs a script tool, the add-on's <c>tools/{name}.js</c>, in Jint: no files, network or CLR, only the
/// <c>ctx</c> of <see cref="ScriptBridge"/>, within <see cref="ScriptLimits"/>. The module exports one function,
/// <c>export default async function (args, ctx)</c>; a string it returns is the answer, anything else is sent as JSON.
/// </summary>
internal sealed partial class ScriptRunner(IPackageStore store, ScriptBridge bridge, ILogger<ScriptRunner> logger)
{
    // The bridge's contract (@homemix/sdk) over one host function: every call answers {value} or {error: {code, message}}.
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
            data: Object.freeze({
              list: (collection, scope) => call('data.list', collection, scope),
              get: (collection, key, scope) => call('data.get', collection, key, scope),
              put: (collection, key, value, options) => call('data.put', collection, key, value, options),
              delete: (collection, key, options) => call('data.delete', collection, key, options).then(() => undefined),
            }),
            members: () => call('members'),
            fetch: (url, init) => call('fetch', String(url), init).then((r) => Object.freeze({
              ok: r.status >= 200 && r.status < 300,
              status: r.status,
              headers: r.headers,
              text: () => Promise.resolve(r.body),
              json: () => Promise.resolve(JSON.parse(r.body)),
            })),
          })
        })()
        const __answer = (r) => typeof r === 'string' ? r : r === undefined || r === null ? '' : JSON.stringify(r)
        """;

    public async Task<Result<string>> RunAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var tool = call.Tool!;
        var source = await ReadAsync(call.Place.Installation.PackageSha256, tool.ScriptPath, cancellationToken);
        if (source is null)
        {
            return Result.Fail(ToolErrors.Script($"{tool.ScriptPath} is missing or larger than {ScriptLimits.SourceBytes / 1024} KB."));
        }

        var context = await bridge.ContextAsync(call.Place, cancellationToken);
        var arguments = JsonSerializer.Serialize(call.Fields);

        // Jint runs synchronously and its ctx calls wait for the use cases, so the run gets a thread of its own.
        return await Task.Run(() => Run(call, source, context, arguments, cancellationToken), cancellationToken);
    }

    private Result<string> Run(ToolCall call, string source, string context, string arguments, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(ScriptLimits.Time);
        var session = bridge.Open(call, limit.Token);
        using var engine = new Engine(o => o
            .LimitMemory(ScriptLimits.Memory)
            .MaxStatements(ScriptLimits.Statements)
            .LimitRecursion(ScriptLimits.Recursion)
            .CancellationToken(limit.Token));
        try
        {
            engine.SetValue("__host", new Func<string, string, string>(session.Call));
            engine.SetValue("__context", context);
            engine.SetValue("__args", arguments);
            engine.Execute(Bootstrap);
            engine.Modules.Add("tool", source);
            var run = engine.Modules.Import("tool").Get("default");
            if (run is not Function)
            {
                return Result.Fail(ToolErrors.Script($"{call.Tool!.ScriptPath} must export a default function (args, ctx)."));
            }

            engine.SetValue("__run", run);
            var answer = engine.Evaluate("Promise.resolve(__run(JSON.parse(__args), ctx)).then(__answer)").UnwrapIfPromise(limit.Token);
            return answer.AsString();
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested && Problem(e) is { } problem)
        {
            LogFailed(logger, call.Place.Installation.AddonId, call.Tool!.Name, problem);
            return Result.Fail(ToolErrors.Script(problem));
        }
    }

    /// <summary>What went wrong, for JARVIS and the add-on's author; null for anything that is not the script's fault.</summary>
    private static string? Problem(Exception e) => e switch
    {
        PromiseRejectedException rejected => Message(rejected.RejectedValue),
        JavaScriptException js => js.Message,
        ExecutionCanceledException or OperationCanceledException or TimeoutException =>
            $"it took longer than {ScriptLimits.Time.TotalSeconds:0} s.",
        MemoryLimitExceededException => $"it used more than {ScriptLimits.Memory / 1024 / 1024} MB.",
        StatementsCountOverflowException => $"it ran more than {ScriptLimits.Statements} statements.",
        RecursionDepthOverflowException => $"it went deeper than {ScriptLimits.Recursion} calls.",
        JintException or Acornima.ParseErrorException => e.Message,
        _ => null,
    };

    private static string Message(JsValue value) =>
        value is Jint.Native.Object.ObjectInstance error && error.Get("message") is { } message && message.IsString()
            ? (error.Get("code") is { } code && code.IsString() ? $"{code.AsString()}: {message.AsString()}" : message.AsString())
            : value.ToString();

    private async Task<string?> ReadAsync(string sha256, string path, CancellationToken cancellationToken)
    {
        await using var file = await store.OpenFileAsync(sha256, path, cancellationToken);
        if (file is null)
        {
            return null;
        }

        var buffer = new byte[ScriptLimits.SourceBytes + 1];
        var read = 0;
        int chunk;
        while (read < buffer.Length && (chunk = await file.ReadAsync(buffer.AsMemory(read), cancellationToken)) > 0)
        {
            read += chunk;
        }

        return read > ScriptLimits.SourceBytes ? null : Encoding.UTF8.GetString(buffer, 0, read);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "The script tool {AddonId}_{Tool} failed: {Problem}")]
    private static partial void LogFailed(ILogger logger, string addonId, string tool, string problem);
}
