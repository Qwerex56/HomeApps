namespace HomeApps.Plugins;

/// <summary>
/// Names for things a plugin only knows by id: the caller's households and the people in one of them, as Households
/// shows them to the caller. For MCP tools, where a model says "Dom" or "Ania" (docs/mcp.md), and for anything else
/// that needs names on the server. Works inside a request with the caller's access token, like <see cref="IHouseholdAccess"/>.
/// </summary>
public interface IHouseholdDirectory
{
    /// <summary>The caller's households (<c>GET /households</c>).</summary>
    Task<IReadOnlyList<HouseholdName>> MyHouseholdsAsync(CancellationToken cancellationToken);

    /// <summary>Everyone with access to the household, guests included (<c>GET /households/{id}/members/names</c>); empty without access.</summary>
    Task<IReadOnlyList<MemberName>> MemberNamesAsync(Guid householdId, CancellationToken cancellationToken);
}
