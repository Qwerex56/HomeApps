using System.ComponentModel;
using System.Globalization;
using Accounts.Application.MyAccount;
using HomeApps.BuildingBlocks.Mcp;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Accounts.Api.Mcp;

/// <summary>MCP tools of Accounts for JARVIS (jarvis-v1.md, section 4). Read only (JV13).</summary>
[McpServerToolType]
internal sealed class AccountTools(ISender sender)
{
    [McpServerTool(Name = "my_profile", Title = "My profile", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The person's own profile: name, email, language, time zone and date of birth.")]
    public async Task<CallToolResult> MyProfile(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyAccountQuery(), cancellationToken);
        return ToolResult.From(result, me => string.Join(Environment.NewLine,
        [
            $"Name: {me.Profile.DisplayName}",
            $"Email: {me.Email}",
            $"Language: {me.Profile.Locale}",
            $"Time zone: {me.Profile.TimeZone}",
            me.Profile.DateOfBirth is { } born
                ? $"Date of birth: {born.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"
                : "Date of birth: not given",
        ]));
    }
}
