using System.Text.Json;
using System.Text.Json.Nodes;
using HomeApps.BuildingBlocks.Mcp;
using Jarvis.Application.Gateway;
using Jarvis.Application.ToolCalls.AwaitConfirmation;
using Jarvis.Application.ToolCalls.CheckRun;
using Jarvis.Application.ToolCalls.RecordCall;
using Jarvis.Infrastructure.Tools;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Jarvis.Infrastructure.Gateway;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Jarvis.Api.Gateway;

/// <summary>
/// The JARVIS gateway (jarvis-v1.md 2.3, 10a): one MCP server in front of every source. Lists tools per person with
/// that person's token and nothing kept between people (HomeMix shows each person only their add-ons); names get the
/// source's prefix (<c>todo_add_task</c>), a second tool with a name already taken is left out and logged; calls and
/// resource reads go to the source that owns the name. A source that does not answer leaves out only its own tools.
/// </summary>
/// <para>
/// In JARVIS's own run (<see cref="Caller.RunId"/>) every call is logged into the run, and a tool that is not marked
/// read-only or explicitly non-destructive is not called but waits for the person's confirmation (JV9): MCP's
/// <c>destructiveHint</c> defaults to true, so an unmarked change counts as destructive.
/// </para>
internal sealed partial class McpGateway(
    IToolSources sources, IHouseholdCeilings ceilings, SourceConnector connector, Caller caller, ISender sender, IMemoryCache cache, ILogger<McpGateway> logger)
{
    public async ValueTask<ListToolsResult> ListToolsAsync(CancellationToken cancellationToken)
    {
        var all = await sources.ListAsync(cancellationToken);
        var scope = caller.HouseholdClaim;
        var satellite = false;
        if (caller.RunId is { } listing && (await sender.Send(new CheckRunQuery(listing, caller.UserId), cancellationToken)) is { IsSuccess: true, Value: { } listed })
        {
            scope ??= listed.HouseholdId;
            satellite = listed.Satellite;
        }

        // H-2, D1: in a household the household's JARVIS settings are a ceiling, so a source it does not allow is not even offered.
        HouseholdCeiling? scoped = scope is { } household ? await ceilings.GetAsync(household, cancellationToken) : null;
        if (scoped is not null)
        {
            all = [.. all.Where(scoped.Allows)];
        }

        var token = await caller.SourceTokenAsync(cancellationToken);
        var perSource = await Task.WhenAll(all.Select(source => ToolsOfAsync(source, token, cancellationToken)));

        var tools = new List<Tool>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var byHousehold = new Dictionary<Guid, HouseholdCeiling>();
        foreach (var (source, tool) in perSource.SelectMany(list => list))
        {
            // A household device's tool belongs to that household: its ceiling applies, and a satellite sees only its own.
            if (HouseholdOf(source, tool) is { } toolHousehold)
            {
                if (caller.HouseholdClaim is { } own && own != toolHousehold)
                {
                    continue;
                }

                if (!byHousehold.TryGetValue(toolHousehold, out var toolCeiling))
                {
                    byHousehold[toolHousehold] = toolCeiling = await ceilings.GetAsync(toolHousehold, cancellationToken);
                }

                if (!toolCeiling.Allows(source))
                {
                    continue;
                }
            }

            // R1: a household satellite is not at anyone's computer, so what is on a person's computer is not offered to it.
            if (satellite && OnComputer(source, tool))
            {
                continue;
            }

            var name = source.Expose(tool.Name);
            if (!names.Add(name))
            {
                LogDuplicate(name, source.Name);
                continue;
            }

            tools.Add(new Tool
            {
                Name = name,
                Title = tool.Title,
                Description = tool.Description,
                InputSchema = tool.InputSchema,
                OutputSchema = tool.OutputSchema,
                Annotations = tool.Annotations,
                Icons = tool.Icons,
                Meta = tool.Meta,
            });
        }

        return new ListToolsResult { Tools = tools };
    }

    public async ValueTask<CallToolResult> CallToolAsync(CallToolRequestParams request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (var source in await sources.ListAsync(cancellationToken))
        {
            if (source.Own(request.Name) is not { } own)
            {
                continue;
            }

            try
            {
                RunScope? run = null;
                if (caller.RunId is { } runId)
                {
                    var checkedRun = await sender.Send(new CheckRunQuery(runId, caller.UserId), cancellationToken);
                    if (checkedRun.IsFailed)
                    {
                        return Failed(ToolResult.Describe(checkedRun.Errors));
                    }

                    run = checkedRun.Value;
                }

                var token = await caller.SourceTokenAsync(cancellationToken);
                var tool = await ToolAsync(source, own, token, caller.UserId, cancellationToken);
                var onComputer = OnComputer(source, tool);
                if (onComputer && run is { Satellite: true })
                {
                    return Failed("This tool is on a person's own computer and cannot be used from a satellite.");
                }

                // H-2, D1: the household of the call caps what may be called. A satellite's own household first, then the
                // household of a household device's tool, the call's household argument, the run's household.
                var inArguments = HouseholdIn(request.Arguments, out var named);
                if (named)
                {
                    return Failed("Pass the household's id (as my_households shows it), not its name.");
                }

                var claim = caller.HouseholdClaim;
                var fromTool = HouseholdOf(source, tool);
                if (claim is { } own1 && ((fromTool ?? inArguments) is { } other && other != own1))
                {
                    return Failed("This assistant acts only in its own household.");
                }

                var household = claim ?? fromTool ?? inArguments ?? run?.HouseholdId;
                var ceiling = household is { } id ? await ceilings.GetAsync(id, cancellationToken) : null;
                if (ceiling is not null && !ceiling.Allows(source))
                {
                    return Failed($"The household's JARVIS settings do not allow {source.Name} tools in this household.");
                }

                if (ceiling is { ReadOnly: true } && tool?.Annotations?.ReadOnlyHint != true)
                {
                    return Failed("The household's JARVIS is read-only in this household: it can look, not change.");
                }

                if (run is not null)
                {
                    return await CallInRunAsync(run.Id, source, own, request, token, tool?.Annotations, cancellationToken);
                }

                // H-4c, until H-7: an external MCP client has no run, so nobody can confirm; an action on a person's computer that asks ("Pytaj") is not done.
                if (onComputer && tool?.Annotations is not { ReadOnlyHint: true } && tool?.Annotations?.DestructiveHint != false)
                {
                    return Failed("This action needs the person's confirmation. Use JARVIS in the HomeApps app.");
                }

                await using var client = await connector.ConnectAsync(source, token, cancellationToken);
                return await client.CallToolAsync(
                    new CallToolRequestParams { Name = own, Arguments = request.Arguments, Meta = request.Meta }, cancellationToken);
            }
            catch (McpException exception)
            {
                return Failed(exception.Message);
            }
            catch (Exception exception) when (Unreachable(exception) && !cancellationToken.IsCancellationRequested)
            {
                LogUnreachable(source.Name, exception);
                return Failed($"{source.Name} did not answer; try again in a moment.");
            }
        }

        return Failed($"There is no tool named {request.Name}.");
    }

    private async Task<CallToolResult> CallInRunAsync(
        Guid runId, ToolSource source, string own, CallToolRequestParams request, string token, ToolAnnotations? annotations, CancellationToken cancellationToken)
    {
        var userId = caller.UserId;
        var arguments = request.Arguments is null ? null : JsonSerializer.Serialize(request.Arguments);
        var readOnly = annotations?.ReadOnlyHint == true;

        if (!readOnly && annotations?.DestructiveHint != false)
        {
            var waiting = await sender.Send(new AwaitConfirmationCommand(runId, userId, request.Name, arguments), cancellationToken);
            return waiting.IsFailed
                ? Failed(ToolResult.Describe(waiting.Errors))
                : ToolResult.Text($"Not done yet: this waits for the person's confirmation on the action card (action {waiting.Value}).");
        }

        await using var client = await connector.ConnectAsync(source, token, cancellationToken);
        var result = await client.CallToolAsync(
            new CallToolRequestParams { Name = own, Arguments = request.Arguments, Meta = request.Meta }, cancellationToken);
        await sender.Send(
            new RecordCallCommand(runId, userId, request.Name, arguments, GatewayToolExecutor.TextOf(result), result.IsError == true, readOnly),
            cancellationToken);
        return result;
    }

    /// <summary>
    /// The tool as the source lists it for this person; kept a few minutes per person, except a device's: its "Pytaj" switch
    /// (<c>destructiveHint</c>) is read from Devices on every call, so turning it off works at once.
    /// </summary>
    private async Task<Tool?> ToolAsync(ToolSource source, string own, string token, Guid userId, CancellationToken cancellationToken)
    {
        var cached = source.Kind != ToolSourceKind.Devices;
        var key = (nameof(ToolAsync), userId, source.Name, own);
        if (cached && cache.TryGetValue(key, out Tool? known))
        {
            return known;
        }

        var tools = await ToolsOfAsync(source, token, cancellationToken);
        var tool = tools.FirstOrDefault(t => t.Tool.Name == own).Tool;
        if (tool is not null && cached)
        {
            cache.Set(key, tool, TimeSpan.FromMinutes(5));
        }

        return tool;
    }

    /// <summary>The household a tool belongs to (<c>_meta</c> "homeapps/household"): only Devices sets it, so only its tools are trusted; never throws.</summary>
    internal static Guid? HouseholdOf(ToolSource source, Tool? tool) =>
        source.Kind == ToolSourceKind.Devices && tool?.Meta?["homeapps/household"] is JsonValue v && v.TryGetValue<string>(out var s) && Guid.TryParse(s, out var id) ? id : null;

    /// <summary>A tool on a person's own computer (<c>_meta</c> "homeapps/purpose": "computer"): only Devices sets it, so only its tools are trusted; never throws.</summary>
    internal static bool OnComputer(ToolSource source, Tool? tool) =>
        source.Kind == ToolSourceKind.Devices && tool?.Meta?["homeapps/purpose"] is JsonValue v && v.TryGetValue<string>(out var s) && s == "computer";

    public async ValueTask<ListResourcesResult> ListResourcesAsync(CancellationToken cancellationToken)
    {
        var all = await sources.ListAsync(cancellationToken);
        var token = await caller.SourceTokenAsync(cancellationToken);
        var perSource = await Task.WhenAll(all.Select(source => ResourcesOfAsync(source, token, cancellationToken)));
        return new ListResourcesResult { Resources = [.. perSource.SelectMany(list => list)] };
    }

    public async ValueTask<ReadResourceResult> ReadResourceAsync(ReadResourceRequestParams request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var colon = request.Uri.IndexOf(':', StringComparison.Ordinal);
        var name = colon > 0 ? request.Uri[..colon] : string.Empty;
        var source = (await sources.ListAsync(cancellationToken)).FirstOrDefault(s => s.Name == name)
            ?? throw new McpException($"There is no resource {request.Uri}.");

        await using var client = await connector.ConnectAsync(source, await caller.SourceTokenAsync(cancellationToken), cancellationToken);
        var read = await client.ReadResourceAsync(new ReadResourceRequestParams { Uri = request.Uri[(colon + 1)..], Meta = request.Meta }, cancellationToken);
        foreach (var content in read.Contents)
        {
            content.Uri = source.ExposeUri(content.Uri);
        }

        return read;
    }

    private async Task<IReadOnlyList<(ToolSource Source, Tool Tool)>> ToolsOfAsync(ToolSource source, string token, CancellationToken cancellationToken)
    {
        try
        {
            await using var client = await connector.ConnectAsync(source, token, cancellationToken);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            return [.. tools.Select(t => (source, t.ProtocolTool))];
        }
        catch (Exception exception) when ((Unreachable(exception) || exception is McpException) && !cancellationToken.IsCancellationRequested)
        {
            LogUnreachable(source.Name, exception);
            return [];
        }
    }

    private async Task<IReadOnlyList<Resource>> ResourcesOfAsync(ToolSource source, string token, CancellationToken cancellationToken)
    {
        try
        {
            await using var client = await connector.ConnectAsync(source, token, cancellationToken);
            if (client.ServerCapabilities.Resources is null)
            {
                return [];
            }

            var resources = await client.ListResourcesAsync(cancellationToken: cancellationToken);
            return [.. resources.Select(r => new Resource
            {
                Uri = source.ExposeUri(r.ProtocolResource.Uri),
                Name = r.ProtocolResource.Name,
                Title = r.ProtocolResource.Title,
                Description = r.ProtocolResource.Description,
                MimeType = r.ProtocolResource.MimeType,
                Size = r.ProtocolResource.Size,
                Annotations = r.ProtocolResource.Annotations,
                Meta = r.ProtocolResource.Meta,
            })];
        }
        catch (Exception exception) when ((Unreachable(exception) || exception is McpException) && !cancellationToken.IsCancellationRequested)
        {
            LogUnreachable(source.Name, exception);
            return [];
        }
    }

    /// <summary>
    /// The household a tool call names (<c>household</c> or <c>householdId</c>, as JARVIS's prompt tells the model to pass it).
    /// <paramref name="named"/> is true when the argument is a string that is neither an id nor HomeMix's "my account": a
    /// household name, which this gateway cannot cap.
    /// </summary>
    internal static Guid? HouseholdIn(IDictionary<string, JsonElement>? arguments, out bool named)
    {
        named = false;
        foreach (var name in new[] { "household", "householdId" })
        {
            if (arguments is not null && arguments.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (Guid.TryParse(text, out var id))
                {
                    return id;
                }

                if (!string.Equals(text?.Trim(), "my account", StringComparison.OrdinalIgnoreCase))
                {
                    named = true;
                    return null;
                }
            }
        }

        return null;
    }

    private static bool Unreachable(Exception exception) =>
        exception is HttpRequestException or TimeoutException or TaskCanceledException;

    private static CallToolResult Failed(string text) =>
        new() { Content = [new TextContentBlock { Text = text }], IsError = true };

    [LoggerMessage(Level = LogLevel.Warning, Message = "The MCP source {Source} did not answer; its tools are left out of this request.")]
    private partial void LogUnreachable(string source, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tool {Tool} from {Source} is left out: another source already has that name.")]
    private partial void LogDuplicate(string tool, string source);
}
