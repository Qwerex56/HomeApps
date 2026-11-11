using System.ComponentModel;
using HomeApps.BuildingBlocks.Mcp;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HomeApps.Plugins.Hello;

/// <summary>The sample plugin's MCP tools: the same requests as its endpoints, so the same permission checks.</summary>
[McpServerToolType]
internal sealed class GreetingTools(ISender sender)
{
    [McpServerTool(Name = "list_greetings", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists the greetings in a household.")]
    public async Task<CallToolResult> ListGreetings(
        [Description("The household id.")] Guid householdId, CancellationToken cancellationToken) =>
        ToolResult.From(
            await sender.Send(new ListGreetingsQuery(householdId), cancellationToken),
            greetings => greetings.Count == 0 ? "No greetings yet." : string.Join(Environment.NewLine, greetings.Select(g => $"- {g.Text}")));

    [McpServerTool(Name = "say_hello", Destructive = false, OpenWorld = false)]
    [Description("Writes a greeting in a household.")]
    public async Task<CallToolResult> SayHello(
        [Description("The household id.")] Guid householdId,
        [Description("The greeting, up to 100 characters.")] string text,
        CancellationToken cancellationToken) =>
        ToolResult.From(await sender.Send(new SayHelloCommand(householdId, text), cancellationToken), _ => "Greeting written.");
}
