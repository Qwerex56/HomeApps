using System.Text.Json;
using System.Text.Json.Nodes;
using Devices.Application.Common;
using Devices.Application.Tools.CallTool;
using Devices.Application.Tools.ListTools;
using HomeApps.BuildingBlocks.Mcp;
using MediatR;
using ModelContextProtocol.Protocol;

namespace Devices.Api.Mcp;

/// <summary>
/// The MCP tools of Devices are not fixed: they are the actions of the caller's devices connected right now, listed and
/// called by name. An action that wants a "tak" is marked destructive, so JARVIS's gateway asks for it first (S6).
/// </summary>
internal sealed class DeviceToolsServer(ISender sender)
{
    /// <summary>The <c>_meta</c> key naming the household of a household device's tool; JARVIS's gateway caps it by that household (H-2).</summary>
    public const string HouseholdMeta = "homeapps/household";

    /// <summary>The <c>_meta</c> key of a tool on a person's own computer (value <c>computer</c>); JARVIS's gateway keeps it away from household satellites (R1).</summary>
    public const string PurposeMeta = "homeapps/purpose";

    private static readonly JsonElement NoInput = JsonElement.Parse("""{"type":"object","properties":{}}""");

    public async ValueTask<ListToolsResult> ListToolsAsync(CancellationToken cancellationToken)
    {
        var tools = await sender.Send(new ListToolsQuery(), cancellationToken);
        return new ListToolsResult
        {
            Tools = tools.IsFailed ? [] : [.. tools.Value.Select(t => new Tool
            {
                Name = t.Name,
                Description = t.Description,
                InputSchema = t.InputSchema is null ? NoInput : JsonElement.Parse(t.InputSchema),
                Annotations = new ToolAnnotations
                {
                    ReadOnlyHint = t.ReadOnly,
                    DestructiveHint = t.NeedsConfirmation,
                    OpenWorldHint = false,
                },
                Meta = MetaOf(t),
            })],
        };
    }

    private static JsonObject? MetaOf(DeviceTool tool)
    {
        if (tool.HouseholdId is null && !tool.OnComputer)
        {
            return null;
        }

        var meta = new JsonObject();
        if (tool.HouseholdId is { } household)
        {
            meta[HouseholdMeta] = household.ToString();
        }

        if (tool.OnComputer)
        {
            meta[PurposeMeta] = "computer";
        }

        return meta;
    }

    public async ValueTask<CallToolResult> CallToolAsync(CallToolRequestParams request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        JsonElement? input = request.Arguments is { Count: > 0 } arguments ? JsonSerializer.SerializeToElement(arguments) : null;
        var result = await sender.Send(new CallToolCommand(request.Name, input), cancellationToken);
        return ToolResult.From(result, text => string.IsNullOrWhiteSpace(text) ? "Done." : text);
    }
}
