using System.ComponentModel;
using HomeApps.BuildingBlocks.Mcp;
using HomeApps.Plugins.Hosting;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HomeApps.Plugins.Hello;

/// <summary>
/// The sample plugin's MCP tools: the same requests as its endpoints, so the same permission checks. The household is
/// named the way a person names it, or by id (<see cref="HouseholdDirectoryExtensions"/>).
/// </summary>
[McpServerToolType]
internal sealed class GreetingTools(ISender sender, IHouseholdDirectory directory)
{
    [McpServerTool(Name = "list_greetings", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists the greetings in a household.")]
    public async Task<CallToolResult> ListGreetings(
        [Description("The household's name or id.")] string household, CancellationToken cancellationToken)
    {
        var found = await directory.FindHouseholdAsync(household, cancellationToken);
        return found.IsFailed
            ? ToolResult.Error(found.Errors)
            : ToolResult.From(
                await sender.Send(new ListGreetingsQuery(found.Value.Id), cancellationToken),
                greetings => greetings.Count == 0 ? "No greetings yet." : string.Join(Environment.NewLine, greetings.Select(g => $"- {g.Text}")));
    }

    [McpServerTool(Name = "say_hello", Destructive = false, OpenWorld = false)]
    [Description("Writes a greeting in a household.")]
    public async Task<CallToolResult> SayHello(
        [Description("The household's name or id.")] string household,
        [Description("The greeting, up to 100 characters.")] string text,
        CancellationToken cancellationToken)
    {
        var found = await directory.FindHouseholdAsync(household, cancellationToken);
        return found.IsFailed
            ? ToolResult.Error(found.Errors)
            : ToolResult.From(await sender.Send(new SayHelloCommand(found.Value.Id, text), cancellationToken), _ => $"Greeting written in {found.Value.Name}.");
    }
}
