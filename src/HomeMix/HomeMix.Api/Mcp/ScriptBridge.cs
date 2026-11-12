using System.Text.Json;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;
using MediatR;

namespace HomeMix.Api.Mcp;

/// <summary>
/// The server side of a script's <c>ctx</c>: the same contract as the bridge in the browser (docs/homemix.md,
/// "The bridge"), answered with the person's token by the same use cases, so the "never" list holds here too.
/// </summary>
internal sealed class ScriptBridge(ISender sender, IHouseholdRoles households, ICurrentUser currentUser, IHttpClientFactory http)
{
    public const string HttpClientName = "HomeMixScripts";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>What <c>ctx.context()</c> answers, as the browser's host builds it for a frame, without theme or page.</summary>
    public async Task<string> ContextAsync(ToolPlace place, CancellationToken cancellationToken)
    {
        var installation = place.Installation;
        var me = currentUser.RequiredUserId;
        var inHousehold = installation.TargetKind == TargetKind.Household;
        var name = inHousehold
            ? (await households.GetMembersAsync(installation.TargetId, cancellationToken)).FirstOrDefault(m => m.Id == me)?.Name
            : null;
        var permissions = place.Manifest.Permissions
            .Where(p => !inHousehold || (Enum.TryParse<HouseholdRole>(p.MinRole, out var min) && place.Role >= min))
            .Select(p => p.Id);
        return JsonSerializer.Serialize(new
        {
            addon = new { id = place.Manifest.Id, name = place.Manifest.Name.Pl, version = place.Manifest.Version },
            installation = new { id = installation.Id, target = inHousehold ? "Household" : "User" },
            household = inHousehold ? new { id = installation.TargetId, name = place.Name, myRole = place.Role.ToString() } : null,
            user = new { id = me, name = name ?? string.Empty },
            permissions,
            capabilities = installation.Capabilities,
            language = "pl",
            theme = new { dark = false, vars = new { } },
            page = string.Empty,
            widget = (object?)null,
        }, Json);
    }

    public ScriptSession Open(ToolCall call, CancellationToken cancellationToken) =>
        new(call, sender, households, currentUser, http.CreateClient(HttpClientName), cancellationToken);
}
