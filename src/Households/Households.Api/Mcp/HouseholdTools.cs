using System.ComponentModel;
using System.Globalization;
using System.Text;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.BuildingBlocks.Mcp;
using Households.Application.Households;
using Households.Application.Members;
using Households.Application.Members.ListMemberNames;
using Households.Domain.Households;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Households.Api.Mcp;

/// <summary>
/// MCP tools of Households for JARVIS (jarvis-v1.md, section 4). Read only (JV13): an assistant never changes
/// households or memberships, the guard refuses it anyway. Same queries and rules as the HTTP API.
/// </summary>
[McpServerToolType]
internal sealed class HouseholdTools(ISender sender)
{
    [McpServerTool(Name = "my_households", Title = "My households", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists the households the person belongs to, with their role in each and the household id.")]
    public async Task<CallToolResult> MyHouseholds(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyHouseholdsQuery(), cancellationToken);
        return ToolResult.From(result, households =>
        {
            if (households.Count == 0)
            {
                return "The person does not belong to any household.";
            }

            var text = new StringBuilder();
            foreach (var h in households)
            {
                text.Append(CultureInfo.InvariantCulture, $"- {h.Name} (id {h.Id}), role: {h.MyRole}");
                if (h.MemberCount is { } count)
                {
                    text.Append(CultureInfo.InvariantCulture, $", {count} members");
                }

                text.AppendLine();
            }

            return text.ToString().TrimEnd();
        });
    }

    [McpServerTool(Name = "household_members", Title = "Household members", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists who is in a household. Members and up also see roles; a guest sees names only.")]
    public async Task<CallToolResult> HouseholdMembers(
        [Description("The household's name or id, as my_households shows it.")] string household,
        CancellationToken cancellationToken)
    {
        var resolved = await HouseholdLookup.ResolveAsync(sender, household, cancellationToken);
        if (resolved.IsFailed)
        {
            return ToolResult.Error(ToolResult.Describe(resolved.Errors));
        }

        var id = resolved.Value;
        var members = await sender.Send(new ListMembersQuery(id), cancellationToken);
        if (members.IsSuccess)
        {
            return ToolResult.Text(string.Join(Environment.NewLine, members.Value.Select(m =>
                string.Create(CultureInfo.InvariantCulture, $"- {m.Name}: {m.Role}{(m.IsActive ? string.Empty : " (account inactive)")}"))));
        }

        if (!members.Errors.OfType<AppError>().Any(e => e.Code == "household.forbidden"))
        {
            return ToolResult.Error(ToolResult.Describe(members.Errors));
        }

        var names = await sender.Send(new ListMemberNamesQuery(id), cancellationToken);
        return ToolResult.From(names, list => string.Join(Environment.NewLine, list.Select(n => $"- {n.Name}")));
    }
}
