using System.Text.Json;
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
    private static readonly JsonElement NoInput = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement.Clone();

    public async ValueTask<ListToolsResult> ListToolsAsync(CancellationToken cancellationToken)
    {
        var tools = await sender.Send(new ListToolsQuery(), cancellationToken);
        return new ListToolsResult
        {
            Tools = tools.IsFailed ? [] : [.. tools.Value.Select(t => new Tool
            {
                Name = t.Name,
                Description = t.Description,
                InputSchema = t.InputSchema is null ? NoInput : JsonDocument.Parse(t.InputSchema).RootElement.Clone(),
                Annotations = new ToolAnnotations
                {
                    ReadOnlyHint = t.ReadOnly,
                    DestructiveHint = t.NeedsConfirmation,
                    OpenWorldHint = false,
                },
            })],
        };
    }

    public async ValueTask<CallToolResult> CallToolAsync(CallToolRequestParams request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        JsonElement? input = request.Arguments is { Count: > 0 } arguments ? JsonSerializer.SerializeToElement(arguments) : null;
        var result = await sender.Send(new CallToolCommand(request.Name, input), cancellationToken);
        return ToolResult.From(result, text => string.IsNullOrWhiteSpace(text) ? "Done." : text);
    }
}
